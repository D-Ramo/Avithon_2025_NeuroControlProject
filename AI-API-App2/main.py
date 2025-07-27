import asyncio
import json
import socket
import struct
import threading
import time
import logging
import numpy as np
from collections import deque
from datetime import datetime, timezone
from typing import Optional, Dict, Any, List
from scipy.signal import welch
from scipy.stats import skew, kurtosis, entropy
import joblib
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import uvicorn

# Import configuration
from config import (
    Config, 
    get_config_for_environment, 
    LOGGING_CONFIG, 
    FEATURE_EXTRACTION_CONFIG,
    PERFORMANCE_CONFIG,
    API_CONFIG
)

# Configure logging using settings from config
logging.basicConfig(
    level=getattr(logging, LOGGING_CONFIG['level']),
    format=LOGGING_CONFIG['format'],
    handlers=[
        logging.FileHandler(LOGGING_CONFIG['log_file']),
        logging.StreamHandler()
    ] if LOGGING_CONFIG['console_output'] else [logging.FileHandler(LOGGING_CONFIG['log_file'])]
)
logger = logging.getLogger(__name__)

class EEGSample(BaseModel):
    Time: str
    Event: int
    Callsign: Optional[str]
    RawEEG: float
    Attention: int = 0
    Meditation: int = 0
    Delta: int = 0
    Theta: int = 0
    LowAlpha: int = 0
    HighAlpha: int = 0
    LowBeta: int = 0
    HighBeta: int = 0
    LowGamma: int = 0
    HighGamm: int = 0

class MentalStateResponse(BaseModel):
    state_of_mind: str  # "active" or "inactive"
    confidence: float
    last_update: str
    prediction_raw: int
    samples_processed: int
    uptime_seconds: float
    total_events: str  
    state1: str  
    state2: str 
    state3: str  
    state4: str  

class SystemStatus(BaseModel):
    status: str
    multicast_connected: bool
    model_loaded: bool
    samples_received: int
    predictions_made: int
    current_buffer_size: int
    last_packet_time: Optional[str]
    errors: List[str]

class FeatureExtractor:
    def __init__(self, config: Config):
        self.config = config
        self.fs = config.sampling_rate
        self.bands = config.bands
        self.nperseg = FEATURE_EXTRACTION_CONFIG['nperseg']
    
    def extract_features(self, signal: np.ndarray) -> Dict[str, float]:
        """Extract EEG features matching the training pipeline"""
        try:
            # Frequency analysis
            freqs, psd = welch(signal, fs=self.fs, nperseg=min(len(signal), self.nperseg))
            total_power = np.trapezoid(psd, freqs)

            features = {}

            # Band powers
            for band, (fmin, fmax) in self.bands.items():
                idx = (freqs >= fmin) & (freqs <= fmax)
                power = np.trapezoid(psd[idx], freqs[idx])
                features[band] = power
                features[f'rel_{band}'] = power / total_power if total_power > 0 else 0

            # Spectral entropy
            psd_norm = psd / np.sum(psd) if np.sum(psd) > 0 else psd
            features['spec_entropy'] = entropy(psd_norm)

            # Hjorth parameters
            activity = np.var(signal)
            mobility = np.sqrt(np.var(np.diff(signal)) / activity) if activity > 0 else 0
            complexity = np.sqrt(np.var(np.diff(np.diff(signal))) / np.var(np.diff(signal))) if np.var(np.diff(signal)) > 0 else 0

            features['hjorth_activity'] = activity
            features['hjorth_mobility'] = mobility
            features['hjorth_complexity'] = complexity

            # Statistical features
            features['skewness'] = skew(signal)
            features['kurtosis'] = kurtosis(signal)
            features['beta_alpha_ratio'] = features['beta'] / features['alpha'] if features['alpha'] > 0 else 0

            return features
        
        except Exception as e:
            logger.error(f"Feature extraction error: {e}")
            return {}

# Real-time EEG processor
class RealTimeEEGProcessor:
    def __init__(self, config: Config):
        self.config = config
        self.feature_extractor = FeatureExtractor(config)
        self.data_buffer = deque(maxlen=config.buffer_size)
        self.model = None
        self.model_features = FEATURE_EXTRACTION_CONFIG['expected_features']
        
        # State management
        self.current_state = "inactive"
        self.last_prediction = None
        self.last_confidence = 0.0
        self.last_update = None
        self.samples_processed = 0
        self.predictions_made = 0
        self.start_time = time.time()
        
        # Activity time tracking
        self.state_start_time = time.time()
        self.time_active = 0.0
        self.time_inactive = 0.0
        
        # Event tracking (Event=1 to Event=-1)
        self.active_events = {}  # {callsign: start_time}
        self.event_resolution_times = []  # List of resolution times in seconds
        self.total_events_started = 0
        self.total_events_resolved = 0
        
        # Threading
        self.processing_lock = threading.Lock()
        self.should_stop = threading.Event()
        
    def load_model(self, model_path: str) -> bool:
        """Load the trained model"""
        try:
            self.model = joblib.load(model_path)
            logger.info(f"Model loaded successfully from {model_path}")
            logger.info(f"Expected features: {self.model_features}")
            return True
        except Exception as e:
            logger.error(f"Failed to load model: {e}")
            return False
    
    def _update_state_times(self, new_state: str):
        """Update activity/inactivity time tracking"""
        current_time = time.time()
        elapsed = current_time - self.state_start_time
        
        if self.current_state == "active":
            self.time_active += elapsed
        else:
            self.time_inactive += elapsed
        
        self.state_start_time = current_time
        self.current_state = new_state
    
    def _process_event(self, sample: Dict[str, Any]):
        """Process event start/stop for resolution time tracking"""
        event = sample.get('Event', 0)
        callsign = sample.get('Callsign')
        timestamp_str = sample.get('Time')
        
        if not callsign or event == 0:
            return
        
        # Parse timestamp
        try:
            timestamp = datetime.fromisoformat(timestamp_str.replace('Z', '+00:00'))
        except:
            timestamp = datetime.now(timezone.utc)
        
        if event == 1:  # Event start
            self.active_events[callsign] = timestamp
            self.total_events_started += 1
            logger.info(f"Event started for {callsign}")
            
        elif event == -1:  # Event resolution
            if callsign in self.active_events:
                start_time = self.active_events.pop(callsign)
                resolution_time = (timestamp - start_time).total_seconds()
                self.event_resolution_times.append(resolution_time)
                self.total_events_resolved += 1
                logger.info(f"Event resolved for {callsign} in {resolution_time:.2f} seconds")
    
    def add_sample(self, sample: Dict[str, Any]):
        """Add new EEG sample to buffer"""
        with self.processing_lock:
            self.data_buffer.append(sample)
            self.samples_processed += 1
            
            # Process event tracking
            self._process_event(sample)
    
    def process_current_window(self) -> Optional[Dict[str, Any]]:
        """Process current window and make prediction"""
        if len(self.data_buffer) < self.config.window_size:
            return None
            
        if self.model is None or self.model_features is None:
            return None
            
        try:
            with self.processing_lock:
                # Extract window
                window_data = list(self.data_buffer)[-self.config.window_size:]
                signal = np.array([sample['RawEEG'] for sample in window_data])
            
            # Extract features
            features = self.feature_extractor.extract_features(signal)
            if not features:
                return None
            
            # Prepare feature vector
            X = np.array([features.get(f, 0.0) for f in self.model_features]).reshape(1, -1)
            
            # Make prediction
            prediction = self.model.predict(X)[0]
            
            # Get prediction probability if available
            confidence = 0.5
            if hasattr(self.model, 'predict_proba'):
                proba = self.model.predict_proba(X)[0]
                confidence = np.max(proba)
            
            # Determine state of mind
            # 0: Negative, 1: Neutral, 2: Positive
            # active = positive/negative, inactive = neutral
            if prediction == 1:  # Neutral
                state_of_mind = "inactive"
            else:  # Positive or Negative
                state_of_mind = "active"
            
            # Update state times if state changed
            if state_of_mind != self.current_state:
                self._update_state_times(state_of_mind)
            
            # Update other state
            self.last_prediction = int(prediction)
            self.last_confidence = float(confidence)
            self.last_update = datetime.now(timezone.utc).isoformat()
            self.predictions_made += 1
            
            result = {
                'state_of_mind': state_of_mind,
                'prediction_raw': int(prediction),
                'confidence': float(confidence),
                'timestamp': self.last_update,
                'features_extracted': len(features),
                'signal_length': len(signal)
            }
            
            # Log predictions at intervals defined in config
            if self.predictions_made % PERFORMANCE_CONFIG['prediction_log_interval'] == 0:
                logger.info(f"Prediction {self.predictions_made}: {prediction} -> {state_of_mind} (confidence: {confidence:.3f})")
            
            return result
            
        except Exception as e:
            logger.error(f"Processing error: {e}")
            return None
    
    def get_current_state(self) -> Dict[str, Any]:
        """Get current mental state with statistics"""
        uptime = time.time() - self.start_time
        
        # Calculate current state duration
        current_time = time.time()
        current_state_duration = current_time - self.state_start_time
        
        # Get total times including current state
        total_active = self.time_active
        total_inactive = self.time_inactive
        
        if self.current_state == "active":
            total_active += current_state_duration
        else:
            total_inactive += current_state_duration
        
        # Calculate average event resolution time
        avg_resolution_time = None
        if self.event_resolution_times:
            avg_resolution_time = sum(self.event_resolution_times) / len(self.event_resolution_times)
        
        # Format the resolution time
        resolution_time_str = f"{avg_resolution_time:.2f}" if avg_resolution_time is not None else "N/A"
        
        return {
            'state_of_mind': self.current_state,
            'confidence': self.last_confidence,
            'last_update': self.last_update or datetime.now(timezone.utc).isoformat(),
            'prediction_raw': self.last_prediction or 1,  # Default to neutral
            'samples_processed': self.samples_processed,
            'uptime_seconds': uptime,
            'total_events': f"Total Events: {self.total_events_started}",
            'state1': f"Total Active Period: {total_active:.2f}",
            'state2': f"Total Inactive Period: {total_inactive:.2f}",
            'state3': f"Average Resolution Time: {resolution_time_str}",
            'state4': f"Total Events Resolved: {self.total_events_resolved}"
        }

# Multicast receiver
class MulticastReceiver:
    def __init__(self, config: Config, processor: RealTimeEEGProcessor):
        self.config = config
        self.processor = processor
        self.socket = None
        self.is_connected = False
        self.should_stop = threading.Event()
        self.samples_received = 0
        self.last_packet_time = None
        self.errors = []
        
    def connect(self) -> bool:
        """Connect to multicast group"""
        try:
            # Create socket
            self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            
            # Bind to multicast group
            self.socket.bind(('', self.config.multicast_port))
            
            # Join multicast group
            mreq = struct.pack("4sl", socket.inet_aton(self.config.multicast_group), socket.INADDR_ANY)
            self.socket.setsockopt(socket.IPPROTO_IP, socket.IP_ADD_MEMBERSHIP, mreq)
            
            self.socket.settimeout(PERFORMANCE_CONFIG['processing_timeout'])
            
            self.is_connected = True
            logger.info(f"Connected to multicast group {self.config.multicast_group}:{self.config.multicast_port}")
            return True
            
        except Exception as e:
            error_msg = f"Failed to connect to multicast: {e}"
            logger.error(error_msg)
            self.errors.append(error_msg)
            return False
    
    def start_receiving(self):
        """Start receiving data in a separate thread"""
        thread = threading.Thread(target=self._receive_loop, daemon=True)
        thread.start()
        return thread
    
    def _receive_loop(self):
        """Main receiving loop"""
        logger.info("Starting multicast receive loop")
        
        while not self.should_stop.is_set():
            try:
                if not self.is_connected:
                    time.sleep(1)
                    continue
                
                # Receive data
                data, addr = self.socket.recvfrom(1024)
                self.last_packet_time = datetime.now(timezone.utc).isoformat()
                
                # Parse JSON
                packet_str = data.decode('utf-8').strip()
                sample_data = json.loads(packet_str)
                
                # Validate and add to processor
                sample = EEGSample(**sample_data)
                self.processor.add_sample(sample.dict())
                
                self.samples_received += 1
                
                # Log at intervals defined in config
                if self.samples_received % PERFORMANCE_CONFIG['log_interval'] == 0:
                    logger.info(f"Received {self.samples_received} samples")
                
            except socket.timeout:
                continue
            except json.JSONDecodeError as e:
                error_msg = f"JSON decode error: {e}"
                logger.warning(error_msg)
                self._add_error(error_msg)
            except Exception as e:
                error_msg = f"Receive error: {e}"
                logger.error(error_msg)
                self._add_error(error_msg)
                time.sleep(1)
    
    def _add_error(self, error_msg: str):
        """Add error to list, maintaining max size"""
        self.errors.append(error_msg)
        if len(self.errors) > PERFORMANCE_CONFIG['max_errors_to_keep']:
            self.errors.pop(0)
    
    def disconnect(self):
        self.should_stop.set()
        if self.socket:
            try:
                self.socket.close()
            except:
                pass
        self.is_connected = False
        logger.info("Disconnected from multicast")
    
    def get_status(self) -> Dict[str, Any]:
        return {
            'connected': self.is_connected,
            'samples_received': self.samples_received,
            'last_packet_time': self.last_packet_time,
            'errors': self.errors[-PERFORMANCE_CONFIG['max_errors_to_keep']:]  
        }

class EEGMonitorApp:
    def __init__(self, config: Config):
        self.config = config
        self.processor = RealTimeEEGProcessor(config)
        self.receiver = MulticastReceiver(config, self.processor)
        self.processing_thread = None
        
    def start(self):
        logger.info("Starting EEG Monitor")
        logger.info(f"Configuration: Sampling Rate={self.config.sampling_rate}Hz, "
                   f"Window Size={self.config.window_size}, Buffer Size={self.config.buffer_size}")
        
        if not self.processor.load_model(self.config.model_path):
            logger.error("Failed to load model. System cannot start.")
            return False
        
        if not self.receiver.connect():
            logger.error("Failed to connect to multicast. System cannot start.")
            return False
        
        self.receiver.start_receiving()
        
        self.processing_thread = threading.Thread(target=self._processing_loop, daemon=True)
        self.processing_thread.start()
        
        logger.info("EEG Monitor started successfully")
        return True
    
    def _processing_loop(self):
        while True:
            try:
                result = self.processor.process_current_window()
                if result:
                    logger.debug(f"Processed window: {result['state_of_mind']}")
                
                time.sleep(self.config.stride / self.config.sampling_rate)
                
            except Exception as e:
                logger.error(f"Processing loop error: {e}")
                time.sleep(1)
    
    def stop(self):
        logger.info("Stopping EEG Monitor")
        self.receiver.disconnect()
        self.processor.should_stop.set()
    
    def get_system_status(self) -> Dict[str, Any]:
        receiver_status = self.receiver.get_status()
        
        return {
            'status': 'running' if receiver_status['connected'] else 'disconnected',
            'multicast_connected': receiver_status['connected'],
            'model_loaded': self.processor.model is not None,
            'samples_received': receiver_status['samples_received'],
            'predictions_made': self.processor.predictions_made,
            'current_buffer_size': len(self.processor.data_buffer),
            'last_packet_time': receiver_status['last_packet_time'],
            'errors': receiver_status['errors']
        }

# Initialize FastAPI app
app = FastAPI(title="EEG Mental State Monitor", version="1.0.0")

# Add CORS middleware using config settings
app.add_middleware(
    CORSMiddleware,
    allow_origins=API_CONFIG['cors_origins'], 
    allow_credentials=API_CONFIG['cors_credentials'],
    allow_methods=API_CONFIG['cors_methods'],
    allow_headers=API_CONFIG['cors_headers'],
)

# Load configuration based on environment
config = get_config_for_environment()
logger.info(f"Loaded configuration for environment: {config}")

# Global app instance
monitor_app = EEGMonitorApp(config)

@app.on_event("startup")
async def startup_event():
    """Start the EEG monitoring system"""
    success = monitor_app.start()
    if not success:
        logger.error("Failed to start EEG monitoring system")

@app.on_event("shutdown") 
async def shutdown_event():
    monitor_app.stop()

@app.get("/state", response_model=MentalStateResponse)
async def get_mental_state():
    """Get current mental state information"""
    try:
        state_data = monitor_app.processor.get_current_state()
        return MentalStateResponse(**state_data)
    except Exception as e:
        logger.error(f"API error: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/status", response_model=SystemStatus)
async def get_system_status():
    """Get system status and health information"""
    try:
        status_data = monitor_app.get_system_status()
        return SystemStatus(**status_data)
    except Exception as e:
        logger.error(f"Status API error: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/config")
async def get_current_config():
    """Get current configuration settings"""
    try:
        return {
            'sampling_rate': config.sampling_rate,
            'window_size': config.window_size,
            'stride': config.stride,
            'buffer_size': config.buffer_size,
            'multicast_group': config.multicast_group,
            'multicast_port': config.multicast_port,
            'model_path': config.model_path,
            'frequency_bands': config.bands
        }
    except Exception as e:
        logger.error(f"Config API error: {e}")
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    # Configuration can be overridden here if needed for direct execution
    # config.multicast_group = "230.0.0.0"
    # config.multicast_port = 4446          
    # config.model_path = "xgboost_model.pkl"  
    
    uvicorn.run(
        "main:app", 
        host=config.api_host,
        port=config.api_port,
        log_level=API_CONFIG['log_level'],
        reload=API_CONFIG['reload']  
    )
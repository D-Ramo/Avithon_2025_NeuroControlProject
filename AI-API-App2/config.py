"""
Configuration settings for EEG Mental State Monitor
"""
from dataclasses import dataclass
from typing import Dict

@dataclass
class Config:
    """Configuration class for EEG monitoring system"""
    
    # =============================================================================
    # EEG PROCESSING SETTINGS
    # =============================================================================
    sampling_rate: int = 200        # Hz - EEG sampling frequency
    window_size: int = 400          # Number of samples per analysis window
    stride: int = 100               # Step size between windows
    buffer_size: int = 2000         # Maximum buffer size for incoming data
    
    # =============================================================================
    # MULTICAST NETWORK SETTINGS  
    # =============================================================================
    multicast_group: str = "230.0.0.0"  # Multicast IP address
    multicast_port: int = 4446           # Multicast port number
    
    # =============================================================================
    # API SERVER SETTINGS
    # =============================================================================
    api_host: str = "0.0.0.0"       # API server host (0.0.0.0 for all interfaces)
    api_port: int = 8000            # API server port
    
    # =============================================================================
    # MODEL SETTINGS
    # =============================================================================
    model_path: str = "xgboost_model.pkl"  # Path to trained model file
    
    # =============================================================================
    # EEG FREQUENCY BANDS
    # =============================================================================
    bands: Dict[str, tuple] = None
    
    def __post_init__(self):
        """Initialize frequency bands if not provided"""
        if self.bands is None:
            self.bands = {
                'delta': (0.5, 4),    # Delta waves: 0.5-4 Hz (deep sleep)
                'theta': (4, 8),      # Theta waves: 4-8 Hz (drowsiness, meditation)
                'alpha': (8, 12),     # Alpha waves: 8-12 Hz (relaxed awareness)
                'beta': (12, 30),     # Beta waves: 12-30 Hz (active concentration)
                'gamma': (30, 45)     # Gamma waves: 30-45 Hz (high-level cognitive)
            }

# =============================================================================
# LOGGING CONFIGURATION
# =============================================================================
LOGGING_CONFIG = {
    'level': 'INFO',  # Options: DEBUG, INFO, WARNING, ERROR, CRITICAL
    'format': '%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    'log_file': 'eeg_monitor.log',
    'console_output': True
}

# =============================================================================
# FEATURE EXTRACTION SETTINGS
# =============================================================================
FEATURE_EXTRACTION_CONFIG = {
    'nperseg': 400,  # Length of each segment for Welch's method
    'expected_features': [
        'beta_alpha_ratio', 'delta', 'gamma', 'hjorth_activity', 
        'hjorth_complexity', 'hjorth_mobility', 'kurtosis', 'rel_alpha', 
        'rel_beta', 'rel_delta', 'rel_gamma', 'rel_theta', 'skewness', 
        'spec_entropy', 'theta'
    ]
}

# =============================================================================
# SYSTEM PERFORMANCE SETTINGS
# =============================================================================
PERFORMANCE_CONFIG = {
    'processing_timeout': 1.0,      # Seconds to wait for socket operations
    'max_errors_to_keep': 10,       # Number of recent errors to store
    'log_interval': 100,            # Log status every N samples
    'prediction_log_interval': 50   # Log predictions every N predictions
}

# =============================================================================
# API SETTINGS
# =============================================================================
API_CONFIG = {
    'cors_origins': ["*"],          # CORS allowed origins (* for all)
    'cors_credentials': True,       # Allow credentials in CORS
    'cors_methods': ["*"],          # Allowed HTTP methods
    'cors_headers': ["*"],          # Allowed headers
    'reload': False,                # Auto-reload on code changes (dev only)
    'log_level': "info"            # uvicorn log level
}

# =============================================================================
# CONVENIENCE FUNCTION TO GET CONFIG
# =============================================================================
def get_config() -> Config:
    """
    Get configuration instance with default settings.
    Modify this function to load from environment variables or config files.
    """
    return Config()

def get_production_config() -> Config:
    """Get configuration optimized for production environment"""
    config = Config()
    # Production-specific settings
    config.api_host = "127.0.0.1"  # More restrictive host
    return config

def get_development_config() -> Config:
    """Get configuration optimized for development environment"""
    config = Config()
    # Development-specific settings
    config.buffer_size = 1000  # Smaller buffer for testing
    config.window_size = 200   # Smaller window for faster processing
    return config

# =============================================================================
# ENVIRONMENT-BASED CONFIG LOADING
# =============================================================================
import os

def get_config_for_environment() -> Config:
    """Load configuration based on environment variable"""
    env = os.getenv('EEG_ENVIRONMENT', 'development').lower()
    
    if env == 'production':
        return get_production_config()
    elif env == 'development':
        return get_development_config()
    else:
        return get_config()
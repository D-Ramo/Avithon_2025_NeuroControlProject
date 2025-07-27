using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;



namespace SimATC
{
	public class SignalReader
	{
		public SerialPort serialPort;
		static Queue<byte> byteQueue = new Queue<byte>();
		public List<SignalClass> Signals = new List<SignalClass>();
		public string fileName = "0Out.txt";
		public DateTime StartRecording;
		IPEndPoint remoteEP;
		UdpClient udpClient = new UdpClient();
		bool udp_activated = false;
	
		public void InitiateSocket(string ip_src, string ip_dst, int port)
		{
			if (ip_src != null && ip_src != "" && port != 0)
			try
			{
					IPAddress multicastAddress = IPAddress.Parse(ip_dst);
					IPAddress localAddress = IPAddress.Parse(ip_src);

					 remoteEP = new IPEndPoint(multicastAddress, port);
					

					udpClient.Client.SetSocketOption(
					SocketOptionLevel.IP,
					SocketOptionName.MulticastInterface,
					localAddress.GetAddressBytes()) ;
					udp_activated = true;

					//udpClient.Client.Bind(new IPEndPoint(localInterface, 0))

					udpClient.Ttl = 5;
				}
			catch (Exception ex) { }
		}

		public void SendUdpSignal(string str_data)
		{
			if (udp_activated)
			try
			{
					
					byte[] data = Encoding.UTF8.GetBytes(str_data);
				    udpClient.Send(data, data.Length, remoteEP);//, server, port);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error sending UDP message: " + ex.Message);
			}
		}
	


		public void AddRecord (SignalClass record)
		{
			try
			{
				Signals.Add(record);

				var settings = new JsonSerializerSettings
				{
					DateTimeZoneHandling = DateTimeZoneHandling.Unspecified
				};


				var json_string = JsonConvert.SerializeObject(record,settings);
				json_string.Replace("+00:00", "");
				SendUdpSignal(json_string);
				File.AppendAllText(fileName, json_string + "\r\n");

			}
			catch (Exception ex) { }
		}


		public SignalReader()
		{
			
		}


		public SignalReader(List<SignalClass> signals)
		{
			Signals = signals;
		}

		public void Close()
		{
			if (serialPort != null )serialPort.Close();
		}
		public void Open(string port)
		{
			try
			{
				serialPort = new SerialPort(port, 57600);
				serialPort.DataReceived += SerialDataReceived;
				serialPort.Open();
				StartRecording = DateTime.UtcNow;
				fileName = "0_OUT " + DateTime.UtcNow.ToString("dd_MM_yyyy hh_mm_ss") + ".txt";
			}
			catch (Exception ex) { }

		}

		void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			int bytesToRead = serialPort.BytesToRead;
			byte[] incoming = new byte[bytesToRead];
			serialPort.Read(incoming, 0, bytesToRead);

			foreach (byte b in incoming)
				byteQueue.Enqueue(b);

			try
			{
				ProcessQueue();
			} catch (Exception ex) { }
		}


		void ProcessQueue()
		{
			while (byteQueue.Count >= 4) // Minimum: 2 sync + length + checksum
			{
				byte[] temp = byteQueue.ToArray();

				// Look for sync bytes
				int syncIndex = -1;
				for (int i = 0; i < temp.Length - 1; i++)
				{
					if (temp[i] == 0xAA && temp[i + 1] == 0xAA)
					{
						syncIndex = i;
						break;
					}
				}

				if (syncIndex == -1)
				{
					byteQueue.Clear(); // no sync found, drop junk
					return;
				}

				// Remove junk before sync
				for (int i = 0; i < syncIndex; i++)
					byteQueue.Dequeue();

				// Now queue starts at 0xAA 0xAA
				if (byteQueue.Count < 3) return; // not enough for length

				byteQueue.Dequeue(); // 0xAA
				byteQueue.Dequeue(); // 0xAA
				byte payloadLength = byteQueue.Dequeue();

				if (byteQueue.Count < payloadLength + 1) // +1 for checksum
				{
					// Not enough data yet
					byteQueue.Enqueue(0xAA); // restore sync we took out (optional)
					return;
				}

				byte[] payload = new byte[payloadLength];
				for (int i = 0; i < payloadLength; i++)
					payload[i] = byteQueue.Dequeue();

				byte checksum = byteQueue.Dequeue();

				// Optionally validate checksum
				byte computedChecksum = 0;
				foreach (byte b in payload)
					computedChecksum += b;
				computedChecksum = (byte)(~computedChecksum & 0xFF);

				if (computedChecksum != checksum)
				{
					Console.WriteLine("⚠️ Bad checksum. Skipping packet.");
					continue;
				}

				var Signal = ParsePayload(payload);
				if (Signal != null) AddRecord(Signal);
			}
		}

		public SignalClass ParsePayload(byte[] p)
		{

			var NewSignal = new SignalClass();
			int i = 0;
			bool success = false;
			while (i < p.Length)
			{
				byte code = p[i];
				int len = 1;
				int shift = 0;
				switch (code)
				{
					case 0x80: shift = 1; len = p[i + 1];  break;
					case 0x83: shift = 1; len = p[i + 1]; break;
					case 0x04: break;
					case 0x02: break;					
					case 0x05: break;

				}		
				if (i + len > p.Length) break;

				byte[] data = new byte[len];
				Array.Copy(p, i + shift + 1, data, 0, len);
				i = i + shift + len + 1;

				switch (code)
				{
					case 0x02: Console.WriteLine($"PoorSignal: {data[0]}"); NewSignal.SignalLevel = data[0]; break;
					case 0x04: Console.WriteLine($"Attention: {data[0]}"); NewSignal.Attention = data[0];  break;
					case 0x05: Console.WriteLine($"Meditation: {data[0]}"); NewSignal.Meditation = data[0]; break;
					case 0x16: Console.WriteLine($"BlinkStrength: {data[0]}"); NewSignal.Blink = data[0]; break;
					case 0x20: Console.WriteLine($"BatteryLevel: {data[0]}"); break;
					case 0x80:
						short raw = (short)((data[0] << 8) | data[1]);
						NewSignal.RawEEG = raw;
						//Console.WriteLine($"EEGRaw: {raw}");
						success = true;
						break;
					case 0x83:
						Console.WriteLine("EEGPower:");
						string[] bands = { "Delta", "Theta", "LowAlpha", "HighAlpha", "LowBeta", "HighBeta", "LowGamma", "HighGamma" };
						for (int b = 0; b < 8; b++)
						{
							int val = (data[b * 3] << 16) | (data[b * 3 + 1] << 8) | data[b * 3 + 2];
							Console.WriteLine($"  {bands[b]}: {val}");
							switch (b)
							{
								case 0: NewSignal.Delta = val; success = true; break;
								case 1: NewSignal.Theta = val; success = true; break;
								case 2: NewSignal.LowAlpha = val; success = true; break;
								case 3: NewSignal.HighAlpha = val; success = true; break;
								case 4: NewSignal.LowBeta = val; success = true; break;
								case 5: NewSignal.HighBeta = val; success = true; break;
								case 6: NewSignal.LowGamma = val; success = true; break;
								case 7: NewSignal.HighGamm = val; success = true; break;
							}

						}
						break;
					default: Console.WriteLine($"Unrecognized code: 0x{code:X2}"); break;
				}
			}

			if (success) return NewSignal; else return null;
		}
	}
	public enum RecordTypeEnum { SIGNAL, EVENT };
	public class SignalClass
	{
		public DateTime Time { get; set; }
		public int Event { get; set; }		// "1" - issue happened, "-1" -  issue resolved
		public string Callsign { get; set; }
		public int RawEEG { get; set; }

		public int Attention { get; set; }
		public int Meditation { get; set; }	

		public int Blink { get; set; }
		public int SignalLevel { get; set; }
		public int Delta { get; set; }
		public int Theta { get; set; }
		public int LowAlpha { get; set; }
		public int HighAlpha { get; set; }
		public int LowBeta { get; set; }
		public int HighBeta { get; set; }
		public int LowGamma { get; set; }
		public int HighGamm { get; set; }
		public SignalClass()
		{
			Time = DateTime.UtcNow;
		}

	}






}

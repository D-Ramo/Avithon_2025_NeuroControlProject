using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.ComponentModel;
using System.Data;
using System.Drawing;

using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using SimATC;

namespace EegReader
{

	

	internal class Program
	{

		public static string src_ip;
		public static string dst_ip;
		public static int port;
		public static string com_port = "COM4";
		static SignalReader SignalReader;

		


		static List<SignalClass> Signals = new List<SignalClass>();
		static void Main(string[] args)
		{
			
			com_port = args[0];
			src_ip = args[1];
			dst_ip = args[2];
			port = Convert.ToInt16(args[3]);
			SignalReader = new SignalReader(Signals);
			SignalReader.InitiateSocket(src_ip,dst_ip, port);
			if (!args.Contains("-t"))SignalReader.Open(com_port);
			Random RND = new Random();

			while (true)
			{
				Thread.Sleep(50);
				if (args.Contains("-t"))
				{
					var NewSignal = new SignalClass();
					NewSignal.Time = DateTime.UtcNow;
					NewSignal.Attention = RND.Next(255);
					NewSignal.Meditation = RND.Next(255);
					NewSignal.HighAlpha = RND.Next(255);
					NewSignal.LowAlpha = RND.Next(255);
					NewSignal.LowBeta = RND.Next(255);
					NewSignal.HighBeta = RND.Next(255);
					NewSignal.LowGamma = RND.Next(255);
					NewSignal.HighGamm = RND.Next(255);
					NewSignal.Delta = RND.Next(255);
					NewSignal.Theta = RND.Next(255);
					NewSignal.Blink = RND.Next(255);
					NewSignal.SignalLevel = RND.Next(255);
					SignalReader.AddRecord(NewSignal);
				}
			}
		}
	}
}

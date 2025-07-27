using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimATC
{
    internal static class Program
    {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		public static string src_ip;
		public static string dst_ip;
		public static int port;
		public static string com_port = "COM4";
        [STAThread]
        static void Main(string[] args)
        {
			com_port = args[0];
			src_ip = args[1];
			dst_ip = args[2];
			port = Convert.ToInt16(args[3]);

			Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SimATC
{
	public partial class GraphForm : Form
	{
		public List<MyGraphClass> Graphs = new List<MyGraphClass>();
		public Thread thGraph;

		public SignalReader SignalReader;
		public GraphForm()
		{
			InitializeComponent();
		}

		public void Start( SignalReader SignalReader)
		{
			this.SignalReader = SignalReader;
			thGraph = new Thread(() => { SignalReader.Signals.DrawAllEegDiagrams(Graphs); });
			thGraph.Start();
		}

		private void pictureBox1_Click(object sender, EventArgs e)
		{

		}

		private void GraphForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			thGraph.Abort();
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
		
			
		}

		private void GraphForm_Resize(object sender, EventArgs e)
		{
			try
			{
				Graphs.ResizeGraphs();
			}
			catch (Exception ex) { }
		}

		private void pictureBox5_Click(object sender, EventArgs e)
		{

		}

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			openFileDialog1.ShowDialog();
		}

		private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
		{


			var fileName = openFileDialog1.FileName;

			var lines = File.ReadAllLines(fileName);
			SignalReader.Signals.Clear();
			foreach (var line in lines)
			{
				try 
				{
					var NewRecord = JsonConvert.DeserializeObject<SignalClass>(line);
					SignalReader.Signals.Add(NewRecord);
				}
				catch (Exception ex) { }
			}

		}
	}
}

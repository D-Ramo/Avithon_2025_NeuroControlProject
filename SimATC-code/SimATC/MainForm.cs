using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;

namespace SimATC
{
    public partial class Form1 : Form
    {

        public ExerciseClass Exercise = new ExerciseClass();
        //public List<RecordClass> Records = new List<RecordClass>();
        public SignalReader SignalReader;// = new SignalReader();
		public List<SignalClass> Signals = new List<SignalClass>();

		public Thread thGraph;
        public Form1()
        {
            InitializeComponent();
            SignalReader = new SignalReader(Signals);
            Exercise.InitGraph(pictureBox1);
            Exercise.InitSimExercise(40, SignalReader);
            Exercise.Speed = trackBar1.Value;



            // Add FL to menu
            for (int i = 17; i < 40; i++)
                contextMenuStrip1.Items.Add("FL " + (i * 10).ToString());


            // Run Drawing task
            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        Exercise.CleanCanvas();
                        Exercise.DrawMap();
                        Exercise.DrawFlights();
                    }
                    catch (Exception ex) { };
                    Thread.Sleep(50);

                }
            }).Start();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            try
            {
                Exercise.InitGraph(pictureBox1);
            }
            catch (Exception ex) { }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Exercise.IsAlertGenerating = !Exercise.IsAlertGenerating;                      
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

			Exercise.IsRunning = false;


            // Initialize Signal Reader
			Signals.Clear();
			SignalReader.Close();
            SignalReader.Open(Program.com_port);
            SignalReader.InitiateSocket(Program.src_ip, Program.dst_ip, Program.port);


            // Initialize timer
            timer1.Stop();
            timer1.Interval = Convert.ToInt16(toolStripTextBox1.Text) * 1000;
            timer1.Start();


            // Initialize new exercise
			Exercise.Flights.Clear();
            Exercise.IsAlertGenerating = false;
            var N = Convert.ToInt16(toolStripTextBox3.Text);
			Exercise.AddRandomFlights(N);
            Exercise.InitFlightNumber = N;
            Exercise.AlertsNumberPerCycle = Convert.ToInt16(toolStripTextBox4.Text);
			Exercise.StartGame = DateTime.UtcNow;
			Exercise.Speed = trackBar1.Value;
			Exercise.IsRunning = true;
            
            
		}

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            Exercise.Speed = trackBar1.Value;
        }

    

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            //Diaplay Signal Graph Diagrams from signal reader module
            //in new window

            var form = new GraphForm();
            form.Show();
			form.Graphs.Add(new MyGraphClass(form.pictureBox1));
			form.Graphs.Add(new MyGraphClass(form.pictureBox2));
			form.Graphs.Add(new MyGraphClass(form.pictureBox3));
			form.Graphs.Add(new MyGraphClass(form.pictureBox4));
			form.Graphs.Add(new MyGraphClass(form.pictureBox5));
			form.Graphs.Add(new MyGraphClass(form.pictureBox6));
			form.Graphs.Add(new MyGraphClass(form.pictureBox7));
			form.Graphs.Add(new MyGraphClass(form.pictureBox8));
			form.Start(SignalReader);
 		}

		private void toolStripButton2_Click(object sender, EventArgs e)
		{
            SignalReader.Close();
            Exercise.IsRunning = false;

		}

		private void Form1_FormClosed(object sender, FormClosedEventArgs e)
		{
            Process.GetCurrentProcess().Kill();
		}

		private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
		{
            Exercise.mX = e.X;
            Exercise.mY = e.Y;
		}


		private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
            try
            {                
                var FL = e.ClickedItem.ToString().Replace("FL", "").Replace(" ", "");
                var Flight = Exercise.Flights.FirstOrDefault(x => x.CALLSIGN == Exercise.SelectedFlight);
                if (Flight != null) Flight.SetAFL(Convert.ToInt32(FL));
            }
            catch (Exception ex) { }
		}

		private void contextMenuStrip1_Click(object sender, EventArgs e)
		{
            Exercise.SelectedFlight = Exercise.MouseOverFlight;
		}



		private void timer2_Tick(object sender, EventArgs e)
		{
            if (Exercise.IsRunning && Exercise.IsAlertGenerating)
            {
                var every_sec = Convert.ToInt16(toolStripTextBox2.Text);             
                if (every_sec != 0 && DateTime.UtcNow.Second % every_sec == 0)  Exercise.RandomGenerateEvent();       
            }
		}

		private void pictureBox1_Click(object sender, EventArgs e)
		{

		}

		private void Form1_Load(object sender, EventArgs e)
		{

		}
	}
}

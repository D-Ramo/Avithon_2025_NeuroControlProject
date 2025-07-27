using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimATC
{
    [Flags]
    public enum FlightStatusEnum { NONE = 0, STCA = 1, MSAW =2, LB = 4};
    public enum GameStatus { STOP, PAUSE, PLAY};
    public class ExerciseClass
    {
        public List<FlightClass> Flights { get; set; } = new List<FlightClass>();

        public int InitFlightNumber = 0;
        public int AlertsNumberPerCycle = 4;

		SignalReader signalReader;

        public string MouseOverFlight = "";
		public string SelectedFlight = "";

		Random RND = new Random();
        public bool IsRunning = false;
        public bool IsInitialized =  false;

        public DateTime Time;
        public int Speed = 1; // Replay speed 1 = x1
        public int H; // Area in NM Height
        public int W; // Area in NM Width
        public bool IsAlertGenerating = false;

		public DateTime StartGame = new DateTime();


        public int mX = 0;
        public int mY = 0;

        // Canvas to draw flights 
        public PictureBox pb;
        public Graphics gFlights;     
        public int gH;
        public int gW;
        public int Xc = 0;
        public int Yc = 0;

        public double TimeInterval = 20;
        public Thread thGame;
        public Image map;



        public void CleanCanvas()
        {
			gFlights.FillRectangle(Brushes.White, 0, 0, gW, gH);
			gFlights.FillRectangle(Brushes.Wheat, gW / 2 - W, gH / 2 - H, 2 * W, 2 * H);
			gFlights.DrawRectangle(new Pen(Brushes.Black, 2), gW / 2 - W, gH / 2 - H, 2 * W, 2 * H);
		}
        public void DrawMap()
        {

            int xoff = map.Width / 2 - gW / 2;
			int yoff = map.Height / 2 - gH / 2;
			gFlights.DrawImage(map, -xoff, -yoff);
        }


        public void DrawFlights()
        {
			//####################### Draw Flights

			gFlights.DrawString($"{Time.ToString("HH:mm:ss")}\r\nFlights:{Flights.Count}",
                new Font("Arial",12), Brushes.Black, 5,5);

			bool isMouseOver = false;

            foreach (var flight in Flights.ToList())
            {
                try
                {
                    Brush color = Brushes.Black;
                    Font font = new  Font("Arial", 9);
                    string alert = "";
                    if (flight.STATUS != 0)
                    {

                        if (Time.Millisecond < 500)
                        {
                            color = Brushes.Red;
							font = new Font("Arial", 8, FontStyle.Bold);
						}
                        else
                        {
                            color = Brushes.Transparent;
							font = new Font("Arial", 8, FontStyle.Bold);
						}
						
                        alert = $"\r\n{flight.STATUS}";
                    }

                    var fX = (int)(flight.X + gW / 2);
                    var fY = (int)(flight.Y + gH / 2);

                    if (!isMouseOver && Math.Pow(fX - mX, 2) + Math.Pow(fY - mY, 2) < 80) 
                        MouseOverFlight = flight.CALLSIGN;

                    foreach (var position in flight.Track.FindAll(x=>x.Time > Time.AddSeconds(-10)).ToList()) 
                        gFlights.FillRectangle(Brushes.Gray, (int)(position.X + gW / 2), (int)(position.Y + gH / 2), 3, 3);



                    string str = $"{flight.CALLSIGN}\r\nFL:{flight.AFL}\r\nCFL:{flight.CFL}{alert}";
                    var Last = flight.Track.Last();
                    
                    gFlights.DrawString(str, font, color, (int)(Last.X + gW / 2) - 58, (int)(Last.Y + gH / 2) - 48);
                    gFlights.DrawLine(new Pen(Brushes.Gray, 2), (int)fX, (int)fY, (int)fX - 10, (int)fY - 10);
					gFlights.FillRectangle(color, (int)(Last.X + gW / 2) - 2, (int)(Last.Y + gH / 2) - 2, 5, 5);
					if (flight.CALLSIGN == MouseOverFlight)	gFlights.FillRectangle(Brushes.DarkBlue, (int)(flight.X + gW / 2) -5, (int)(flight.Y + gH / 2) -5 , 10, 10);
				}
                catch (Exception ex)
                { 
                
                }                
            }
           Flights.RemoveAll(x=>x.X > W || x.X < -W || x.Y > H || x.Y < -H);

			pb.Invoke((MethodInvoker)(() =>
			{
				pb.Refresh();
			}));

		}





        public void InitGraph(PictureBox pbFlights)
        {
            pb = pbFlights;
            pbFlights.Image = new Bitmap(pbFlights.Width, pbFlights.Height);
            gH = pbFlights.Height;
            gW = pbFlights.Width;
            gFlights = Graphics.FromImage(pbFlights.Image);
            map = Bitmap.FromFile("map.png");
            H = gH / 2;
            W = gW / 2;
        }

        public void InitSimExercise(int n, SignalReader reader, int H = 400, int W = 400)
        {
            signalReader = reader;
            IsInitialized = false;
            IsRunning = false;
			this.H = H;
			this.W = W;
			H = gH / 2;
			W = gW / 2;

			
            Time = DateTime.UtcNow;
            Flights.Clear();
            reader.Signals.Clear();
            AddRandomFlights(n);
            thGame = new Thread(MainSimLoop);
            thGame.Start();
            IsInitialized = true;
        }


        public void RandomGenerateEvent()
        {
            try
            {


                // FLight Level Bust Generation
                var n = RND.Next(1, AlertsNumberPerCycle);
                for (int i = 0; i < n; i++)
                {
                    var nFlight = RND.Next(Flights.Count);
                    if (nFlight < Flights.Count) Flights[nFlight].CFL = RND.Next(17, 40) * 10;
                }
             

				// Add  new flights if less than 40
				AddRandomFlights(InitFlightNumber - Flights.Count);
            }
            catch (Exception ex) { }
        }


        public void AddRandomFlights(int n)
        {
            if (n == 0) return;
            string[] airlines = new string[5] { "SVA", "ETH", "KNE", "FDB", "KLM" };
            for (int i = 0; i < n; i++)
            {
                var callsign = airlines[RND.Next(5)] + RND.Next(999).ToString().PadLeft(3, '0');
                Flights.Add(new FlightClass(H, W, callsign, Time, RND));
            }
        }


        public void MainSimLoop()
        {
            while (true)
            {
                if (IsRunning && IsInitialized)
                { 
                    try
                    {
                        foreach (var flight in Flights)
                            if (flight != null)
                            {
                                flight.Move(Time, Speed);
                                var PrevStatus = flight.STATUS;

                                foreach (var flight1 in Flights)
                                    if (flight1 != null && flight1.CALLSIGN != flight.CALLSIGN) flight.CheckAlerts(flight1);

                                if (PrevStatus != flight.STATUS)
                                {

                                    var Record = new SignalClass();
                                    Record.Time = DateTime.UtcNow;
                                    Record.Event = 1;
                                    Record.Callsign = flight.CALLSIGN;
                                    if (flight.STATUS == 0) Record.Event = -1;

                                    // Records.Add(Record);

                                    signalReader.AddRecord(Record);
                                }
                            }
                        Time = DateTime.UtcNow;
                    }
                    catch (Exception e) 
                    { 
                    
                    }
                   
                }
				Thread.Sleep(100);
			}
        }   
    }

    public class FlightClass
    {
       
        public string CALLSIGN { get; set; }                
        public double CFL { get; set; }    
        public double AFL { get; set; }
        public double S_FL { get; set; }
        public double HDG { get; set; }
        public double S_HDG { get; set; }
        public double SPD { get; set; }
        public double S_SPD { get; set; }

        public double X { get => Track.Last().X; }
        public double Y { get => Track.Last().Y; }

		public override string ToString()
        {
            return $"{CALLSIGN}/{X}/{Y}/{S_FL}";

        }

        public FlightStatusEnum STATUS { get; set; }
        public List<PositionClass> Track { get; set; } = new List<PositionClass>();

        public FlightClass() { }


        public void SetAFL(int cfl)
        {
            AFL = cfl;
            Track.Last().A = AFL;
            S_FL = cfl;
        }

        public void CheckAlerts(FlightClass Flight)
        {


            double xx = (Flight.Track.Last().X - Track.Last().X);
            double yy = (Flight.Track.Last().Y - Track.Last().Y);

            

            if (Math.Abs(AFL - Flight.AFL) < 1000 && Math.Sqrt(xx * xx + yy * yy) < 10)
                STATUS |= FlightStatusEnum.STCA;
            else
                STATUS &= ~FlightStatusEnum.STCA;

            if (CFL < 170)
                STATUS |= FlightStatusEnum.MSAW;
            else
                STATUS &= ~FlightStatusEnum.MSAW;

            if (Math.Abs(CFL - AFL) > 3)
                STATUS |= FlightStatusEnum.LB;
            else
                STATUS &= ~FlightStatusEnum.LB;

            

            
        }

        public FlightClass(int H, int W, string callsign, DateTime initTime, Random RND)
        {
            

            var Position = new PositionClass();
            Position.X =  RND.Next(-W, W);
            Position.Y =  RND.Next(-H, H);
            Position.A =  Math.Round(RND.Next(170, 400)/10.0) * 10;
            Position.Time = initTime;
            Track.Add(Position);
            CALLSIGN = callsign;

            AFL = Position.A;
            CFL = Position.A;
            S_FL = Position.A;
            SPD =  RND.Next(350, 550);
            HDG =  RND.Next(0, 360);
        }


        public void Move(DateTime time, double speed)
        {
            var Last = Track.LastOrDefault();
            var New = new PositionClass();
            var T = time.Subtract(Last.Time).TotalSeconds;
            New.X = Last.X + speed * SPD/3600 * Math.Cos(HDG / 180 * Math.PI) * T;
            New.Y = Last.Y + speed * SPD/3600 * Math.Sin(HDG / 180 * Math.PI) * T;
            New.A = Last.A;
            New.Time = time;
            Track.Add(New);
            var TimeDiff = time.Subtract(Last.Time).TotalSeconds;
            double AltDiff = S_FL - Last.A;            
        }
    }

    public class PositionClass
    {
        public DateTime Time { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double A { get; set; }
    }

  
 

   
   
  
}

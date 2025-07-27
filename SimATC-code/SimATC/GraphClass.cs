using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimATC
{
	public class DrawSignalClass
	{
		public DateTime time;
		public int value;
	}
	public class MyGraphClass
	{
		public int Interval = 400;
		Graphics g;
		public PictureBox pb;
		int H;
		int W;


		public MyGraphClass(PictureBox pb)
		{
			InitGraph(pb);
		}
		public void InitGraph(PictureBox pb = null)
		{
			if (pb != null) this.pb = pb;
			this.pb.Image = new Bitmap(this.pb.Width, this.pb.Height);
			g = Graphics.FromImage(this.pb.Image);
			H = this.pb.Height;
			W = this.pb.Width;
		}
		public void CleanDraw(Color color)
		{
			var Brush = new SolidBrush(color);
			g.FillRectangle(Brush, 0, 0, W, H);
		}
		public void DrawDiagram(List<DrawSignalClass> Range, DateTime StartTime, Color color, string text = "", bool bar = false, bool range = true, int width = 2, double Min = 0, double Max = 0)
		{
			if (Range.Count == 0) return;
			if (Min == 0 && Max == 0)
			{
				Max = Range.Max(x => x.value) * 1.05;
				Min = Range.Min(x => x.value) * 0.95;
			}
			double yScale = (double)H / (Max - Min);
			double xScale = (double)W / Interval;

			var Brush = new SolidBrush(color);
			var Pen = new Pen(Brush, width);

			
			if (range)
			{
				g.DrawString(Max.ToString(), new Font("Arial", 12), Brushes.Black, W - 50, 0);
				g.DrawString(Min.ToString(), new Font("Arial", 12), Brushes.Black, W - 50, H - 20);
			}

			double xx = 0;
			double yy = 0;
			double xx1 = 0;
			double yy1 = 0;
			 
			for (int i = 0; i < Range.Count; i++)
			{
				xx1 = Range[i].time.Subtract(StartTime).TotalSeconds * xScale;
				yy1 = H - (Range[i].value - Min) * yScale;
				if (i > 0 && !bar) g.DrawLine(Pen, (int)xx, (int)yy, (int)xx1, (int)yy1);

				if (bar) g.DrawLine(Pen, (int)xx1, (int)yy1, (int)xx1, H/2 );
				xx = xx1;
				yy = yy1;
			}

			g.DrawString(text, new Font("Arial", 12, FontStyle.Bold), Brush, W / 2 - 50, 0);
		}



	}
	public static class MyGraphExtension
	{
		public static void ResizeGraphs(this List<MyGraphClass> graphs)
		{
			foreach (var graph in graphs)
				graph.InitGraph();
		}

		public static void RefreshGraphs(this List<MyGraphClass> graphs)
		{
			foreach (var graph in graphs)
			{
				//graph.pb.Refresh();
				
				graph.pb.Invoke((MethodInvoker)(() =>
				{
					graph.pb.Refresh();
				}));
			}
		}


		public static void DrawAllEegDiagrams(this List<SignalClass> Signals, List<MyGraphClass> Graphs)
		{
			while (true)
			{
				if (Signals.Count > 0)
					try
					{
						DateTime LastTime = DateTime.UtcNow;
						var Last = Signals.LastOrDefault();

						if (Last != null) LastTime = Last.Time;

						var Range = Signals.FindAll(x => x!= null && x.Time > LastTime.AddSeconds(-Graphs.First().Interval));
						


						if (Range.Count > 0)
						{
							Color bg = Color.White;
							DateTime StartTime = Range.First().Time;
							List<DrawSignalClass> DrawRange;
							Graphs[7].CleanDraw(bg);
							DrawRange = Range.FindAll(x => x.Event != 0).Select(p => new DrawSignalClass { value = (int)p.Event, time = p.Time }).ToList();
							Graphs[7].DrawDiagram(DrawRange, StartTime, Color.Black, "EVENTS", true, true, 4, -1,1);



							Graphs[0].CleanDraw(bg);
							DrawRange = Range.FindAll(x => x.RawEEG != 0).Select(p => new DrawSignalClass { value = p.RawEEG, time = p.Time }).ToList();
							Graphs[0].DrawDiagram(DrawRange, StartTime, Color.Black, "RAW EEG");

							Graphs[1].CleanDraw(bg);
							DrawRange = Range.FindAll(x => x.HighAlpha != 0).Select(p => new DrawSignalClass { value = p.HighAlpha, time = p.Time }).ToList();
							
							
							var Min = DrawRange.Min(x => x.value);
							var Max = DrawRange.Max(x => x.value);

							Graphs[1].DrawDiagram(DrawRange, StartTime, Color.YellowGreen, "H_ALPHA", false, true, 2, Min, Max);
							DrawRange = Range.FindAll(x => x.LowAlpha != 0).Select(p => new DrawSignalClass { value = p.LowAlpha, time = p.Time }).ToList();
							Graphs[1].DrawDiagram(DrawRange, StartTime, Color.Red, "                      L_ALPHA", false, false, 2, Min, Max);



							Graphs[2].CleanDraw(bg);
							DrawRange = Range.FindAll(x => x.HighBeta != 0).Select(p => new DrawSignalClass { value = p.HighBeta, time = p.Time }).ToList();
							Min = DrawRange.Min(x => x.value);
							Max = DrawRange.Max(x => x.value);


							Graphs[2].DrawDiagram(DrawRange, StartTime,Color.YellowGreen, "H_BETA", false, true, 2, Min, Max);
							DrawRange = Range.FindAll(x => x.LowBeta != 0).Select(p => new DrawSignalClass { value = p.LowBeta, time = p.Time }).ToList();
							Graphs[2].DrawDiagram(DrawRange, StartTime, Color.Red, "                      L_BETA", false, false, 2, Min, Max);


							Graphs[3].CleanDraw(bg);
							DrawRange = Range.FindAll(x => x.HighGamm != 0).Select(p => new DrawSignalClass { value = p.HighGamm, time = p.Time }).ToList();
							Min = DrawRange.Min(x => x.value);
							Max = DrawRange.Max(x => x.value);

							Graphs[3].DrawDiagram(DrawRange,StartTime	 ,Color.YellowGreen, "H_GAMMA", false, true, 2, Min, Max);
							DrawRange = Range.FindAll(x => x.LowGamma != 0).Select(p => new DrawSignalClass { value = p.LowGamma, time = p.Time }).ToList();
							Graphs[3].DrawDiagram(DrawRange, StartTime, Color.Red, "                      L_GAMMA", false, false, 2, Min, Max);








							Graphs[4].CleanDraw(bg);
							DrawRange = Range.FindAll(x => x.Theta != 0).Select(p => new DrawSignalClass { value = p.Theta, time = p.Time }).ToList();
							Graphs[4].DrawDiagram(DrawRange, StartTime, Color.YellowGreen, "THETA");

							Graphs[5].CleanDraw(bg);
							DrawRange = Range.FindAll(x => x.Delta != 0).Select(p => new DrawSignalClass { value = p.Delta, time = p.Time }).ToList();
							Graphs[5].DrawDiagram(DrawRange, StartTime, Color.Red, "DELTA");





							Graphs[6].CleanDraw(bg);
							DrawRange = Range.FindAll(x => x.Attention != 0).Select(p => new DrawSignalClass { value = p.Attention, time = p.Time }).ToList();						
							Graphs[6].DrawDiagram(DrawRange, StartTime, Color.YellowGreen, "ATTENT",true, true, 2, 0, DrawRange.Max(x=>x.value));

							DrawRange = Range.FindAll(x => x.Meditation != 0).Select(p => new DrawSignalClass { value = p.Meditation, time = p.Time }).ToList();
							Graphs[6].DrawDiagram(DrawRange, StartTime, Color.Red, "                    MEDIT", false, false, 2, 0, 255);

						}
					}
					catch (Exception ex) { }

				RefreshGraphs(Graphs);
				Thread.Sleep(100);
			}
		}

	}
}

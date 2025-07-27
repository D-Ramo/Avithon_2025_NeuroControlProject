using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimATC
{
	internal class Class1
	{
	}
	// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
	public class EventPeriod
	{
		public int event_id { get; set; }
		public int event_type { get; set; }
		public int start_sample { get; set; }
		public int end_sample { get; set; }
		public int duration_samples { get; set; }
		public double duration_seconds { get; set; }
	}

	public class EventSummary
	{
		public int total_events { get; set; }
		public List<EventPeriod> event_periods { get; set; }
	}

	public class Metadata
	{
		public int total_predictions { get; set; }
		public DateTime analysis_date { get; set; }
		public double duration_seconds { get; set; }
		public SamplingInfo sampling_info { get; set; }
		public string input_file { get; set; }
		public int window_size { get; set; }
		public int stride { get; set; }
		public string event_method { get; set; }
		public bool use_natural_events { get; set; }
		public List<string> features_used { get; set; }
	}

	public class Negative
	{
		public int count { get; set; }
		public double percentage { get; set; }
	}

	public class Neutral
	{
		public int count { get; set; }
		public double percentage { get; set; }
	}

	public class Positive
	{
		public int count { get; set; }
		public double percentage { get; set; }
	}

	public class Prediction
	{
		public int index { get; set; }
		public DateTime timestamp { get; set; }
		public int prediction_value { get; set; }
		public string prediction_label { get; set; }
		public int event_detected { get; set; }
		public DateTime window_start_time { get; set; }
	}

	public class Root
	{
		public Metadata metadata { get; set; }
		public List<Prediction> predictions { get; set; }
		public Summary summary { get; set; }
	}

	public class SamplingInfo
	{
		public int window_size_samples { get; set; }
		public int stride_samples { get; set; }
		public int sampling_rate_hz { get; set; }
	}

	public class StateDistribution
	{
		public Positive Positive { get; set; }
		public Negative Negative { get; set; }
		public Neutral Neutral { get; set; }
	}

	public class Summary
	{
		public StateDistribution state_distribution { get; set; }
		public EventSummary event_summary { get; set; }
	}


}

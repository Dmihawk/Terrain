using System.Diagnostics;

namespace Visualiser.Utilities
{
	public class Timer
	{
		private Stopwatch _stopwatch;
		private float _ticksPerMillisecond;
		private long _lastFrameTime = 0L;

		public float FrameTime { get; private set; }
		public float CumulativeFrameTime { get; private set; }

		public bool Initialise()
		{
			var result = Stopwatch.IsHighResolution;
			result &= Stopwatch.Frequency != 0;

			if (result)
			{
				_ticksPerMillisecond = Stopwatch.Frequency / 1000.0f;
				_stopwatch = Stopwatch.StartNew();
			}

			return result;
		}

		public void Frame()
		{
			var currentTime = _stopwatch.ElapsedTicks;
			var timeDifference = currentTime - _lastFrameTime;

			FrameTime = timeDifference / _ticksPerMillisecond;
			CumulativeFrameTime += FrameTime;

			_lastFrameTime = currentTime;
		}
	}
}
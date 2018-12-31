using System;

namespace Visualiser.Utilities
{
	public class FrameCounter
	{
		private int _count;
		private TimeSpan _startTime;

		public int FPS { get; private set; }

		public void Initialise()
		{
			FPS = 0;
			_count = 0;
			_startTime = DateTime.Now.TimeOfDay;
		}

		public void Frame()
		{
			_count++;

			var secondsPassed = (DateTime.Now.TimeOfDay - _startTime).Seconds;

			if (secondsPassed >= 1)
			{
				FPS = _count;
				_count = 0;
				_startTime = DateTime.Now.TimeOfDay;
			}
		}
	}
}

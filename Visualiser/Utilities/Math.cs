namespace Visualiser.Utilities
{
	public static class Math
	{
		
		public static float Clamp(this float value, float min, float max)
		{
			return (value < min) ? min : (value > max) ? max : value;
		}

		public static int Clamp(this int value, int min, int max)
		{
			return (value < min) ? min : (value > max) ? max : value;
		}

		public static float RotationLock(this float value)
		{
			while (value < 0)
			{
				value += 360;
			}

			while (value > 360)
			{
				value -= 360;
			}

			return value;
		}
	}
}
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visualiser.Utilities;
using Math = System.Math;

namespace Visualiser.Environment
{
	public static class TimeOfDay
	{
		private static Color4 Sunrise1 = new Color4(88 / 255.0f, 21 / 255.0f, 26 / 255.0f, 1.0f);
		private static Color4 Sunrise2 = new Color4(188 / 255.0f, 41 / 255.0f, 9 / 255.0f, 1.0f);
		private static Color4 Sunrise3 = new Color4(186 / 255.0f, 109 / 255.0f, 9 / 255.0f, 1.0f);
		private static Color4 Sunrise4 = new Color4(113 / 255.0f, 168 / 255.0f, 238 / 255.0f, 1.0f);
		private static Color4 Sunrise5 = new Color4(48 / 255.0f, 114 / 255.0f, 235 / 255.0f, 1.0f);
		private static Color4 Sunrise6 = new Color4(21 / 255.0f, 82 / 255.0f, 198 / 255.0f, 1.0f);
		private static Color4 Sunrise7 = new Color4(5 / 255.0f, 66 / 255.0f, 168 / 255.0f, 1.0f);

		private static Color4 DayTimeBlue = new Color4(0.0f, 0.15f, 0.66f, 1.0f);

		private static Color4 Sunset1 = new Color4(62 / 255.0f, 88 / 255.0f, 121 / 255.0f, 1.0f);
		private static Color4 Sunset2 = new Color4(155 / 255.0f, 165 / 255.0f, 174 / 255.0f, 1.0f);
		private static Color4 Sunset3 = new Color4(220 / 255.0f, 182 / 255.0f, 151 / 255.0f, 1.0f);
		private static Color4 Sunset4 = new Color4(252 / 255.0f, 112 / 255.0f, 1 / 255.0f, 1.0f);
		private static Color4 Sunset5 = new Color4(221 / 255.0f, 114 / 255.0f, 60 / 255.0f, 1.0f);
		private static Color4 Sunset6 = new Color4(173 / 255.0f, 74 / 255.0f, 40 / 255.0f, 1.0f);
		private static Color4 Sunset7 = new Color4(4 / 255.0f, 3 / 255.0f, 8 / 255.0f, 1.0f);

		private static float _currentTime = 0;

		public static Color4 AmbientColour { get; private set; }
		public static Color4 HorizonColour { get; private set; }
		public static Color4 SkyColour { get; private set; }
		public static float CloudBrightness { get; private set; }

		public static void Frame(float frameTime)
		{
			_currentTime += frameTime;

			if (_currentTime > Settings.LengthOfDay.TotalMilliseconds)
			{
				_currentTime -= (float)Settings.LengthOfDay.TotalMilliseconds;
			}

			UpdateSun();
		}

		public static Vector3 GetSunDirection()
		{
			var direction = Vector3.Zero;

			if (_currentTime < Settings.LengthOfDay.TotalMilliseconds / 2)
			{
				var fractionOfDay = _currentTime / (Settings.LengthOfDay.TotalMilliseconds / 2);

				direction.Y = (float)Math.Sin(fractionOfDay * Constants.Pi) * -1.0f;
			}

			return direction;
		}

		private static void UpdateSun()
		{
			if (_currentTime < Settings.LengthOfDay.TotalMilliseconds / 2)
			{
				var fractionOfDay = _currentTime / (Settings.LengthOfDay.TotalMilliseconds / 2);

				if (fractionOfDay <= 0.25f)
				{
					UpdateSunrise((float)fractionOfDay * 4.0f);
				}
				else if (fractionOfDay < 0.75f)
				{
					AmbientColour = Color4.White;
					HorizonColour = DayTimeBlue;
					SkyColour = DayTimeBlue;
					CloudBrightness = 1.0f;
				}
				else
				{
					UpdateSunset(((float)fractionOfDay - 0.75f) * 4.0f);
				}
			}
		}

		private static void UpdateSunrise(float fractionOfSunrise)
		{
			var sunriseSection = 1;

			var temp = fractionOfSunrise;

			while (temp >= 0.125f)
			{
				temp -= 0.125f;
				sunriseSection++;
			}

			var fractionOfSunriseSection = temp / 0.125f;

			Color4 currentColour = Color4.Black;
			Color4 nextColour = Color4.Black;

			switch (sunriseSection)
			{
				case 1:
					currentColour = Color4.Black;
					nextColour = Sunrise1;
					break;

				case 2:
					currentColour = Sunrise1;
					nextColour = Sunrise2;
					break;

				case 3:
					currentColour = Sunrise2;
					nextColour = Sunrise3;
					break;

				case 4:
					currentColour = Sunrise3;
					nextColour = Sunrise4;
					break;

				case 5:
					currentColour = Sunrise4;
					nextColour = Sunrise5;
					break;

				case 6:
					currentColour = Sunrise5;
					nextColour = Sunrise6;
					break;

				case 7:
					currentColour = Sunrise6;
					nextColour = Sunrise7;
					break;

				case 8:
					currentColour = Sunrise7;
					nextColour = DayTimeBlue;
					break;
			}

			HorizonColour = ((nextColour - currentColour) * fractionOfSunriseSection) + currentColour;
			SkyColour = DayTimeBlue * fractionOfSunrise;
			AmbientColour = Color4.White * fractionOfSunrise;
			CloudBrightness = fractionOfSunrise.Clamp(0.015f, 1.0f);
		}

		private static void UpdateSunset(float fractionOfSunset)
		{
			var sunsetSection = 1;

			var temp = fractionOfSunset;

			while (temp >= 0.125f)
			{
				temp -= 0.125f;
				sunsetSection++;
			}

			var fractionOfSunsetSection = temp / 0.125f;

			Color4 currentColour = Color4.Black;
			Color4 nextColour = Color4.Black;

			switch (sunsetSection)
			{
				case 1:
					currentColour = DayTimeBlue;
					nextColour = Sunset1;
					break;

				case 2:
					currentColour = Sunset1;
					nextColour = Sunset2;
					break;

				case 3:
					currentColour = Sunset2;
					nextColour = Sunset3;
					break;

				case 4:
					currentColour = Sunset3;
					nextColour = Sunset4;
					break;

				case 5:
					currentColour = Sunset4;
					nextColour = Sunset5;
					break;

				case 6:
					currentColour = Sunset5;
					nextColour = Sunset6;
					break;

				case 7:
					currentColour = Sunset6;
					nextColour = Sunset7;
					break;

				case 8:
					currentColour = Sunset7;
					nextColour = Color4.Black;
					break;
			}

			HorizonColour = ((nextColour - currentColour) * fractionOfSunsetSection) + currentColour;
			SkyColour = DayTimeBlue * (1.0f - fractionOfSunset);
			AmbientColour = Color4.White * (1.0f - fractionOfSunset);
			CloudBrightness = (1.0f - fractionOfSunset).Clamp(0.015f, 1.0f);
		}
	}
}

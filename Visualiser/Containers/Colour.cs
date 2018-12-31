namespace Visualiser.Containers
{
	public class Colour
	{
		public Colour()
		{

		}

		public Colour(float red, float green, float blue)
		{
			Red = red;
			Green = green;
			Blue = blue;
		}

		public Colour(NamedColour colour)
		{
			SetByName(colour);
		}

		public float Alpha { get; set; }
		public float Red { get; set; }
		public float Green { get; set; }
		public float Blue { get; set; }

		public void ChangeTo(NamedColour colour)
		{
			SetByName(colour);
		}

		private void SetByName(NamedColour colour)
		{
			Red = 0.0f;
			Green = 0.0f;
			Blue = 0.0f;

			switch (colour)
			{
				case NamedColour.Green:
					Green = 1.0f;
					break;

				case NamedColour.Yellow:
					Red = 1.0f;
					Green = 1.0f;
					break;

				case NamedColour.Red:
					Red = 1.0f;
					break;

				case NamedColour.White:
					Red = 1.0f;
					Green = 1.0f;
					Blue = 1.0f;
					break;

				case NamedColour.Black:
				default:
					break;
			}
		}
	}

	public enum NamedColour
	{
		Black,
		Green,
		Yellow,
		Red,
		White,
	}
}

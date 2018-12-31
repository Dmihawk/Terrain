using SharpDX.DXGI;

namespace Visualiser.Containers
{
	public class Dimension
	{
		public Dimension()
		{

		}

		public Dimension(int width, int height)
		{
			Width = width;
			Height = height;
		}

		public int Width { get; set; }
		public int Height { get; set; }

		public float AspectRatio
		{
			get
			{
				var aspectRatio = 0.0f;

				if (Height > 0.0f)
				{
					aspectRatio = (float)Width / Height;
				}

				return aspectRatio;
			}
		}

		public bool SameSizeAs(ModeDescription modeDescription)
		{
			return Width == modeDescription.Width && Height == modeDescription.Height;
		}
	}
}
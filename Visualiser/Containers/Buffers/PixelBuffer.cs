using SharpDX;
using System.Runtime.InteropServices;

namespace Visualiser.Containers.Buffers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct PixelBuffer
	{
		public Vector4 pixelColour;
	}
}

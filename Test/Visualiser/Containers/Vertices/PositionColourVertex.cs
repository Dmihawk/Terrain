using System.Runtime.InteropServices;
using SharpDX;

namespace Visualiser.Containers.Vertices
{
	[StructLayout(LayoutKind.Sequential)]
	public struct PositionColourVertex
	{
		public static int AppendAlignedElement = 12;
		public Vector3 position;
		public Vector4 colour;
	}
}
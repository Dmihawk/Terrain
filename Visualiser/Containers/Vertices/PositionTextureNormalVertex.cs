using SharpDX;
using System.Runtime.InteropServices;

namespace Visualiser.Containers.Vertices
{
	[StructLayout(LayoutKind.Sequential)]
	public struct PositionTextureNormalVertex
	{
		public Vector3 position;
		public Vector2 texture;
		public Vector3 normal;
	}
}

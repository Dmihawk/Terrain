using System.Runtime.InteropServices;
using SharpDX;

namespace Visualiser.Containers.Buffers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct WorldViewProjectionMatrixBuffer
	{
		public Matrix world;
		public Matrix view;
		public Matrix projection;
	}
}
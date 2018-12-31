using System.Runtime.InteropServices;
using SharpDX;

namespace Visualiser.Containers.Buffers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct MatrixBuffer
	{
		public Matrix world;
		public Matrix view;
		public Matrix projection;
	}
}
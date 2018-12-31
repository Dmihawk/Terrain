using SharpDX;
using System.Runtime.InteropServices;

namespace Visualiser.Containers.Buffers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct ViewProjectionMatrixBuffer
	{
		public Matrix view;
		public Matrix projection;
	}
}

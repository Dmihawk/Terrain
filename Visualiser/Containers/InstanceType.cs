using SharpDX;
using System.Runtime.InteropServices;

namespace Visualiser.Containers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct InstanceType
	{
		public Matrix matrix;
		public Vector3 colour;
	}
}

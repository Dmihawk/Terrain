using System.Runtime.InteropServices;

namespace Visualiser.Containers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct ObjectFormat
	{
		public float x;
		public float y;
		public float z;
		public float tu;
		public float tv;
		public float nx;
		public float ny;
		public float nz;
	}
}
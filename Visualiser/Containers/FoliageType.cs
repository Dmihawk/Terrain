using System.Runtime.InteropServices;

namespace Visualiser.Containers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct FoliageType
	{
		public float x;
		public float z;
		public float r;
		public float g;
		public float b;
	}
}

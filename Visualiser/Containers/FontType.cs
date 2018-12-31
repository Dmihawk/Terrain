using System.Runtime.InteropServices;

namespace Visualiser.Containers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct FontType
	{
		public float left;
		public float right;
		public int size;
	}
}
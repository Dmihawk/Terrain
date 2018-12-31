using System.Runtime.InteropServices;
using SharpDX;

namespace Visualiser.Containers.Buffers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct LightBuffer
	{
		public Vector4 ambientColour;
		public Vector4 diffuseColour;
		public Vector3 lightDirection;
		public float padding;
	}
}
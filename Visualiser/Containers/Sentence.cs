using System.Runtime.InteropServices;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace Visualiser.Containers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Sentence
	{
		public Buffer VertexBuffer;
		public Buffer IndexBuffer;
		public int VertexCount;
		public int IndexCount;
		public int MaxLength;
		public Colour Colour;
		public string Text;
		public int PositionY;
	}
}

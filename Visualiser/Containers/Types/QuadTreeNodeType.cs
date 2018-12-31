using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace Visualiser.Containers.Types
{
	[StructLayout(LayoutKind.Sequential)]
	public struct QuadTreeNodeType
	{
		public float positionX;
		public float positionZ;
		public float width;
		public int TriangleCount;
		public Buffer VertexBuffer;
		public Buffer IndexBuffer;
		public XYZType[] vertexArray;
		public QuadTreeNodeType[] Nodes;
	}
}

using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Test.Containers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct VertexType
	{
		internal Vector3 Position;
		internal Vector2 Texture;
	}
}
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Visualiser.Containers.Vertices
{
	[StructLayout(LayoutKind.Sequential)]
	public struct PositionVertex
	{
		public Vector3 position;
	}
}

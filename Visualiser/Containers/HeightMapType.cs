using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Visualiser.Containers
{
	[StructLayout(LayoutKind.Sequential)]
	public struct HeightMapType
	{
		public float x;
		public float y;
		public float z;
	}
}

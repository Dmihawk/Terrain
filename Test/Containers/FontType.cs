using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Test.Containers
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct FontType
	{
		internal float Left;
		internal float Right;
		internal int Size;
	}
}

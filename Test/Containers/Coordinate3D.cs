using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Containers
{
	public class Coordinate3D<T>
	{
		public Coordinate3D()
		{

		}

		public Coordinate3D(T x, T y, T z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public T X { get; set; }
		public T Y { get; set; }
		public T Z { get; set; }

		public void CopyFrom(Coordinate3D<T> coordinate)
		{
			X = coordinate.X;
			Y = coordinate.Y;
			Z = coordinate.Z;
		}
	}
}
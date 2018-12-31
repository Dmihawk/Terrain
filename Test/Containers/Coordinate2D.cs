using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Containers
{
	public class Coordinate2D<T>
	{
		public Coordinate2D()
		{

		}

		public Coordinate2D(T x, T y)
		{
			X = x;
			Y = y;
		}

		public T X { get; set; }
		public T Y { get; set; }
	}
}

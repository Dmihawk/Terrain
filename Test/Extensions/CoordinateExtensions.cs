using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Containers;

namespace Test.Extensions
{
	public static class CoordinateExtensions
	{
		public static Vector3 ToVector3(this Coordinate3D<float> value)
		{
			return new Vector3(value.X, value.Y, value.Z);
		}
	}
}
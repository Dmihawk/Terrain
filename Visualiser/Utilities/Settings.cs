﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visualiser.Containers;

namespace Visualiser.Utilities
{
	public static class Settings
	{
		public static Dimension ScreenSize = new Dimension(1280, 720);
		public static TimeSpan LengthOfDay = TimeSpan.FromMinutes(5);
	}
}

using Visualiser.Utilities;

namespace Visualiser
{
	class Program
	{
		static void Main(string[] args)
		{
			var model = new Model();
			model.Initialise(new Containers.WindowConfiguration()
			{
				Title = "Visualiser Test",
				Size = Settings.ScreenSize,
				VerticalSync = true,
				FullScreen = false
			});

			model.Start();
		}
	}
}
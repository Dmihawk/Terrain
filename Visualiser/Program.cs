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
				Size = new Containers.Dimension(1920, 1080),
				VerticalSync = true,
				FullScreen = false
			});

			model.Start();
		}
	}
}
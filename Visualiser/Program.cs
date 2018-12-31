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
				Size = new Containers.Dimension(1280, 720),
				VerticalSync = true,
				FullScreen = false
			});

			model.Start();
		}
	}
}
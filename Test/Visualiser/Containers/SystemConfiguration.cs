using System.Reflection;
using System.Windows.Forms;

namespace Visualiser.Containers
{
	public class SystemConfiguration
	{
		public string Title { get; set; }
		public Dimension Size { get; set; }

		public static bool FullScreen { get; private set; }
		public static bool VerticalSyncEnabled { get; private set; }
		public static float ScreenDepth { get; private set; }
		public static float ScreenNear { get; private set; }
		public static FormBorderStyle BorderStyle { get; set; }
		public static string ShaderFilepath { get; private set; }
		public static string DataFilePath { get; private set; }
		public static string ObjectFilePath { get; private set; }

		public SystemConfiguration(WindowConfiguration windowConfiguration)
		{
			ScreenDepth = 1000.0f;
			ScreenNear = 0.1f;
			BorderStyle = FormBorderStyle.None;
			ShaderFilepath = @"C:\Code\Git\Database\dbCore\DB Structure\Visualiser\Shaders\";
			DataFilePath = @"C:\Code\Git\Database\dbCore\DB Structure\Visualiser\Data\";
			ObjectFilePath = @"C:\Code\Git\Database\dbCore\DB Structure\Visualiser\Models\";

			FullScreen = windowConfiguration.FullScreen;
			Title = windowConfiguration.Title;
			VerticalSyncEnabled = windowConfiguration.VerticalSync;

			Size = new Dimension()
			{
				Width = FullScreen ? Screen.PrimaryScreen.Bounds.Width : windowConfiguration.Size.Width,
				Height = FullScreen ? Screen.PrimaryScreen.Bounds.Height : windowConfiguration.Size.Height
			};
		}
	}
}

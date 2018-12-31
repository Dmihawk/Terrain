using System.Drawing;
using System.Windows.Forms;
using SharpDX.Windows;
using Visualiser.Containers;
using Visualiser.Graphics;
using Visualiser.Utilities;

namespace Visualiser
{
	public class Model
	{
		private RenderForm _renderForm;

		public Model()
		{

		}

		public SystemConfiguration Configuration { get; private set; }
		public Input Input { get; private set; }
		public Window Window { get; private set; }

		public void Initialise(WindowConfiguration configuration)
		{
			Configuration = new SystemConfiguration(configuration);

			InitialiseWindows(configuration.Title);

			Input = new Input();
			Input.Initialise(configuration.Size, _renderForm.Handle);

			Window = new Window();
			Window.Initialise(configuration.Size, _renderForm.Handle);
		}

		public void Start()
		{
			RenderLoop.Run(_renderForm, () =>
			{
				if (!Frame())
				{
					Stop();
				}
			});
		}

		private void InitialiseWindows(string title)
		{
			var width = Screen.PrimaryScreen.Bounds.Width;
			var height = Screen.PrimaryScreen.Bounds.Height;

			_renderForm = new RenderForm(title)
			{
				ClientSize = new Size(Configuration.Size.Width, Configuration.Size.Height),
				FormBorderStyle = SystemConfiguration.BorderStyle
			};

			_renderForm.Show();

			var x = (width / 2) - (Configuration.Size.Width / 2);
			var y = (height / 2) - (Configuration.Size.Height / 2);

			_renderForm.Location = new Point(x, y);
		}

		private bool Frame()
		{
			var result = Input.Frame() || Input.IsEscapePressed;

			Window.Timer.Frame();

			result &= Window.Frame();

			return result;
		}

		private void Stop()
		{
			_renderForm?.Dispose();
			_renderForm = null;

			Window.Dispose();
			Window = null;

			Input = null;
			Configuration = null;
		}
	}
}
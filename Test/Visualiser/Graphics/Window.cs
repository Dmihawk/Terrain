using Logging;
using SharpDX;
using System;
using Visualiser.Containers;
using Visualiser.Shaders;
using Visualiser.Utilities;

namespace Visualiser.Graphics
{
	public class Window : IDisposable
	{
		private const float PI = 3.14159265358979323846f;

		private DirectX _directX;
		private Camera _camera;
		private Object _object;
		private LightShader _lightShader;
		private Light _light;

		public Window()
		{

		}

		public Timer Timer { get; set; }
		public static float Rotation { get; set; }

		public bool Initialise(Dimension size, IntPtr windowHandle)
		{
			var result = true;

			try
			{
				_directX = new DirectX();
				result &= _directX.Initialise(size, windowHandle);

				Timer = new Timer();
				result &= Timer.Initialise();

				_camera = new Camera();
				_camera.SetPosition(new Coordinate3D<float>(0, 0, -10));

				_object = new Object();
				result &= _object.Initialise(_directX.Device, "Cube.txt", "seafloor.bmp");

				_lightShader = new LightShader();
				result &= _lightShader.Initialise(_directX.Device, windowHandle);

				_light = new Light()
				{
					DiffuseColour = new Vector4(1, 1, 1, 1),
					Direction = new Vector3(0, 0, 1)
				};

				return result;
			}
			catch (Exception ex)
			{
				Log.WriteToFile(ErrorLevel.Error, "Window.Initialise", ex, true);

				return false;
			}
		}

		public void Dispose()
		{
			Timer = null;
			_light = null;
			_camera = null;

			_lightShader.Dispose();
			_lightShader = null;

			_object.Dispose();
			_object = null;

			_directX.Dispose();
			_directX = null;
		}

		public bool Frame()
		{
			Rotate();

			var result = Render(Rotation);

			return result;
		}

		private bool Render(float rotation)
		{
			_directX.BeginScene(new Color4(0.1f, 0f, 0f, 1f));

			_camera.Render();

			var viewMatrix = _camera.ViewMatrix;
			var worldMatrix = _directX.WorldMatrix;
			var projectionMatrix = _directX.ProjectionMatrix;

			//Matrix.RotationYawPitchRoll(Rotation, Rotation, Rotation, out worldMatrix);
			//Matrix.RotationX(rotation, out worldMatrix);
			//Matrix.RotationY(rotation, out worldMatrix);

			_object.Render(_directX.DeviceContext);

			var translationMatrix = worldMatrix * Matrix.RotationY(rotation) * Matrix.Translation((rotation - 180.0f) / 50.0f, 0.0f, 0.0f);
			var result = _lightShader.Render(_directX.DeviceContext, _object.IndexCount, translationMatrix, viewMatrix, projectionMatrix, _object.Texture.TextureResource, _light.Direction, _light.DiffuseColour);

			_directX.EndScene();

			return result;
		}

		public static void Rotate()
		{
			Rotation += PI * 0.01f;

			if (Rotation > 360)
			{
				Rotation -= 360;
			}
		}
	}
}
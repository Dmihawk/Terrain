using SharpDX;
using SharpDX.DirectInput;
using System;
using Visualiser.Containers;
using Visualiser.Graphics.Components;
using Visualiser.Graphics.Objects;
using Visualiser.Shaders;
using Visualiser.Utilities;

namespace Visualiser.Graphics
{
	public class Window : IDisposable
	{
		private DirectX _directX;
		private Camera _camera;
		private Input _input;
		private Player _player;
		private Foliage _foliage;
		private Object _groundModel;
		private ShaderManager _shaderManager;
		private UserInterface _userInterface;
		private FrameCounter _frameCounter;
		private Terrain _terrain;

		public Window()
		{

		}

		public bool Initialise(Dimension size, IntPtr windowHandle)
		{
			var result = true;

			try
			{
				_directX = new DirectX();
				result &= _directX.Initialise(size, windowHandle);

				_input = new Input();
				result &= _input.Initialise(size, windowHandle);

				_shaderManager = new ShaderManager();
				result &= _shaderManager.Initialise(_directX, windowHandle);

				_player = new Player
				{
					Position = new Coordinate3D<float>(0.0f, 1.5f, -4.0f),
					Rotation = new Coordinate3D<float>(15.0f, 0.0f, 0.0f)
				};

				_camera = new Camera();
				_camera.SetPosition(new Coordinate3D<float>(0, 0, -10));
				_camera.Render();
				_camera.RenderBaseViewMatrix();

				_frameCounter = new FrameCounter();
				_frameCounter.Initialise();

				_userInterface = new UserInterface();
				result &= _userInterface.Initialise(_directX, size);

				_terrain = new Terrain();
				result &= _terrain.Initialise(_directX.Device, "heightmap01.bmp", "dirt03.bmp");

				_groundModel = new Object();
				result &= _groundModel.Initialise(_directX.Device, "plane01.txt", "rock015.bmp");

				_foliage = new Foliage();
				result &= _foliage.Initialise(_directX.Device, "grass01.bmp", 500);

				return result;
			}
			catch (Exception ex)
			{
				//Log.WriteToFile(ErrorLevel.Error, "Window.Initialise", ex, true);

				return false;
			}
		}

		public void Dispose()
		{
			_player = null;
			_frameCounter = null;
			_camera = null;

			_foliage?.Dispose();
			_foliage = null;

			_groundModel?.Dispose();
			_groundModel = null;

			_terrain?.Dispose();
			_terrain = null;

			_userInterface?.Dispose();
			_userInterface = null;

			_shaderManager?.Dispose();
			_shaderManager = null;

			_input?.Dispose();
			_input = null;

			_directX?.Dispose();
			_directX = null;
		}

		public bool Frame(float frameTime)
		{
			_frameCounter.Frame();

			var result = _input.Frame();
			result &= HandleInput(frameTime);
			result &= _userInterface.Frame(_frameCounter.FPS, _player.Position, _player.Rotation, _directX.DeviceContext);
			result &= _foliage.Frame(_camera.Position, _directX.DeviceContext);
			result &= Render();

			return result;
		}

		private bool HandleInput(float frameTime)
		{
			_player.Update(frameTime, new MovementInput()
			{
				Forward = _input.IsKeyPressed(Key.W),
				Backward = _input.IsKeyPressed(Key.S),
				Leftward = _input.IsKeyPressed(Key.A),
				Rightward = _input.IsKeyPressed(Key.D),
				Upward = _input.IsKeyPressed(Key.Space),
				Downward = _input.IsKeyPressed(Key.LeftShift),
				LookRight = _input.MouseChange.X,
				LookDown = _input.MouseChange.Y
			});

			_camera.Position = _player.Position;
			_camera.Rotation = _player.Rotation;

			return !_input.IsKeyPressed(Key.Escape);
		}

		private bool Render()
		{
			_directX.BeginScene(new Color4(0.0f, 0.0f, 1.0f, 1.0f));

			_camera.Render();

			var worldMatrix = _directX.WorldMatrix;
			var viewCameraMatrix = _camera.ViewMatrix;
			var projectionMatrix = _directX.ProjectionMatrix;
			var orthoMatrix = _directX.OrthoMatrix;
			var baseViewMatrix = _camera.BaseViewMatrix;

			_groundModel.Render(_directX.DeviceContext);
			_shaderManager.RenderTextureShader(_directX.DeviceContext, _groundModel.IndexCount, worldMatrix, viewCameraMatrix, projectionMatrix, _groundModel.Texture.TextureResource);

			var ambientColour = new Color4(0.05f, 0.05f, 0.05f, 1.0f);
			var diffuseColour = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
			var direction = new Vector3(-0.5f, -1.0f, 0.0f);

			_terrain.Render(_directX.DeviceContext);
			var result = _shaderManager.RenderTerrainShader(_directX.DeviceContext, _terrain.IndexCount, worldMatrix, viewCameraMatrix, projectionMatrix, ambientColour, diffuseColour, direction, _terrain.Texture.TextureResource);

			_directX.EnableSecondBlendState();

			_foliage.Render(_directX.DeviceContext);
			result &= _shaderManager.RenderFoliageShader(_directX.DeviceContext, _foliage.VertexCount, _foliage.InstanceCount, viewCameraMatrix, projectionMatrix, _foliage.Texture.TextureResource);

			_directX.SetAlphaBlending(false);

			_userInterface.Render(_directX, _shaderManager, worldMatrix, baseViewMatrix, orthoMatrix);

			_directX.EndScene();

			return result;
		}
	}
}
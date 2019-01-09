using SharpDX;
using SharpDX.DirectInput;
using System;
using Visualiser.Containers;
using Visualiser.Environment;
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
		private QuadTree _quadTree;
		private Frustrum _frustrum;
		private SkyDome _skyDome;
		private SkyPlane _skyPlane;

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
					Position = new Coordinate3D<float>(31.0f, 18.0f, 7.0f),
					Rotation = new Coordinate3D<float>(11.0f, 23.0f, 0.0f)
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

				_quadTree = new QuadTree();
				result &= _quadTree.Initialise(_terrain, _directX.Device);

				_foliage = new Foliage();
				result &= _foliage.Initialise(_directX.Device, _quadTree, "grass01.bmp", 2500);

				_frustrum = new Frustrum();

				_skyDome = new SkyDome();
				result &= _skyDome.Initialise(_directX.Device);

				_skyPlane = new SkyPlane();
				result &= _skyPlane.Initialze(_directX.Device, "cloud001.bmp", "perturb001.bmp");

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

			_frustrum = null;


			_skyPlane.ShurDown();
			_skyPlane = null;

			_skyDome.ShutDown();
			_skyDome = null;

			_quadTree?.Shutdown();
			_quadTree = null;

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

			TimeOfDay.Frame(frameTime);

			var result = _input.Frame();

			var heightDifference = 0.0f;

			if (_quadTree.GetHeightAtPosition(_camera.Position.X, _camera.Position.Z, out float height))
			{
				heightDifference = _camera.Position.Y - (height + 2.0f);
			}

			result &= HandleInput(frameTime, heightDifference);

			if (_camera.Position.Y < (height + 2.0f))
			{
				_camera.Position.Y = height + 2.0f;
			}

			result &= _userInterface.Frame(_frameCounter.FPS, _player.Position, _player.Rotation, _directX.DeviceContext);
			result &= _foliage.Frame(_camera.Position, _directX.DeviceContext);

			_skyPlane.Frame();

			result &= Render();

			return result;
		}

		private Coordinate2D<float> FindRayIntersectionPoint()
		{
			var inverseViewMatrix = Matrix.Invert(_camera.ViewMatrix);
			var direction = new Vector3()
			{
				X = inverseViewMatrix.M31,
				Y = inverseViewMatrix.M32,
				Z = inverseViewMatrix.M33
			};

			_quadTree.GetHeightAtPosition(_camera.Position.X, _camera.Position.Z, out float playerHeight);
			playerHeight += 2.0f;

			var origin = new Vector3(_camera.Position.X, playerHeight, _camera.Position.Z);

			var unitDirection = Vector3.Normalize(direction) / 5;

			var testDistance = 0.1f;
			var intersectionFound = false;

			Coordinate2D<float> intersection = null;

			while (!intersectionFound && testDistance < 40.0f)
			{
				var currentLocation = origin + (unitDirection * testDistance);

				var heightFound = _quadTree.GetHeightAtPosition(currentLocation.X, currentLocation.Z, out float height);

				if (heightFound && height > currentLocation.Y)
				{
					intersection = new Coordinate2D<float>(currentLocation.X, currentLocation.Z);
					intersectionFound = true;
				}

				testDistance += 0.1f;
			}

			return intersection;
		}

		private bool HandleInput(float frameTime, float heightDifference)
		{
			_player.Update(frameTime, heightDifference, new MovementInput()
			{
				Forward = _input.IsKeyPressed(Key.W),
				Backward = _input.IsKeyPressed(Key.S),
				Leftward = _input.IsKeyPressed(Key.A),
				Rightward = _input.IsKeyPressed(Key.D),
				//Upward = _input.IsKeyPressed(Key.Space),
				//Downward = _input.IsKeyPressed(Key.LeftShift),
				LookRight = _input.MouseChange.X,
				LookDown = _input.MouseChange.Y,
				Jump = _input.IsKeyPressed(Key.Space),
				Sprint = _input.IsKeyPressed(Key.LeftShift)
			});

			_camera.Position = _player.Position;
			_camera.Rotation = _player.Rotation;

			if (_input.IsKeyPressed(Key.E))
			{
				_terrain.ChangeHeightAtPosition(_directX.Device, (int)_player.Position.X, (int)_player.Position.Z, 1.0f);
				_quadTree.Initialise(_terrain, _directX.Device);
			}

			if (_input.IsKeyPressed(Key.Q))
			{
				var intersection = FindRayIntersectionPoint();

				if (intersection != null)
				{
					//_terrain.ChangeHeightAtPosition(_directX.Device, (int)_player.Position.X, (int)_player.Position.Z, -1.0f);
					_terrain.ChangeHeightAtPosition(_directX.Device, (int)intersection.X, (int)intersection.Y, -1.0f);
					//_quadTree.Initialise(_terrain, _directX.Device);
				}
			}

			return !_input.IsKeyPressed(Key.Escape);
		}

		private bool Render()
		{
			_directX.BeginScene(new Color4(0.0f, 0.0f, 0.0f, 1.0f));

			_camera.Render();

			var worldMatrix = _directX.WorldMatrix;
			var viewCameraMatrix = _camera.ViewMatrix;
			var projectionMatrix = _directX.ProjectionMatrix;
			var orthoMatrix = _directX.OrthoMatrix;
			var baseViewMatrix = _camera.BaseViewMatrix;

			//_groundModel.Render(_directX.DeviceContext);
			//_shaderManager.RenderTextureShader(_directX.DeviceContext, _groundModel.IndexCount, worldMatrix, viewCameraMatrix, projectionMatrix, _groundModel.Texture.TextureResource);

			var ambientColour = new Color4(0.05f, 0.05f, 0.05f, 1.0f);
			//var diffuseColour = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
			//var diffuseColour = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
			//var direction = new Vector3(-0.5f, -1.0f, 0.0f);

			var direction = TimeOfDay.GetSunDirection();
			var diffuseColour = TimeOfDay.AmbientColour;

			var result = true;

			Matrix.Translation(_camera.Position.X, _camera.Position.Y, _camera.Position.Z, out worldMatrix);

			_directX.SetCulling(false);
			_directX.SetZBuffer(false);

			_skyDome.Render(_directX.DeviceContext);
			//result = _shaderManager.RenderSkyDomeShader(_directX.DeviceContext, _skyDome.IndexCount, worldMatrix, viewCameraMatrix, projectionMatrix, _skyDome.ApexColour, _skyDome.CenterColour);
			result = _shaderManager.RenderSkyDomeShader(_directX.DeviceContext, _skyDome.IndexCount, worldMatrix, viewCameraMatrix, projectionMatrix, TimeOfDay.SkyColour, TimeOfDay.HorizonColour);

			_directX.SetCulling(true);

			_directX.EnableCloudBlendState();

			_skyPlane.Render(_directX.DeviceContext);
			//result &= _shaderManager.RenderSkyPlaneShader(_directX.DeviceContext, _skyPlane.IndexCount, worldMatrix, viewCameraMatrix, projectionMatrix, _skyPlane.CloudTexture.TextureResource, _skyPlane.PerturbTexture.TextureResource, _skyPlane.Translation, _skyPlane.Scale, _skyPlane.Brightness);
			result &= _shaderManager.RenderSkyPlaneShader(_directX.DeviceContext, _skyPlane.IndexCount, worldMatrix, viewCameraMatrix, projectionMatrix, _skyPlane.CloudTexture.TextureResource, _skyPlane.PerturbTexture.TextureResource, _skyPlane.Translation, _skyPlane.Scale, TimeOfDay.CloudBrightness);

			worldMatrix = _directX.WorldMatrix;

			_directX.SetZBuffer(true);

			_directX.EnableSecondBlendState();

			_foliage.Render(_directX.DeviceContext);
			result &= _shaderManager.RenderFoliageShader(_directX.DeviceContext, _foliage.VertexCount, _foliage.InstanceCount, viewCameraMatrix, projectionMatrix, _foliage.Texture.TextureResource);

			_directX.SetAlphaBlending(false);
			
			_terrain.Render(_directX.DeviceContext);
			result = _shaderManager.RenderTerrainShader(_directX.DeviceContext, _terrain.IndexCount, worldMatrix, viewCameraMatrix, projectionMatrix, ambientColour, diffuseColour, direction, _terrain.Texture.TextureResource);

			_directX.SetZBuffer(false);
			_directX.SetAlphaBlending(true);

			_userInterface.Render(_directX, _shaderManager, worldMatrix, baseViewMatrix, orthoMatrix);

			_directX.SetAlphaBlending(false);
			_directX.SetZBuffer(true);

			_directX.EndScene();

			return result;
		}
	}
}
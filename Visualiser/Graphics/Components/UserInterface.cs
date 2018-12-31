using SharpDX;
using SharpDX.Direct3D11;
using System;
using Visualiser.Containers;
using Visualiser.Shaders;

namespace Visualiser.Graphics.Components
{
	public class UserInterface : IDisposable
	{
		public Font Font { get; set; }
		public Text FpsString { get; set; }
		public int PreviousFPS { get; set; }
		public Text[] PositionStrings { get; set; }
		public Text[] VideoStrings { get; set; }
		public int[] PreviousPositions { get; set; }

		public bool Initialise(DirectX directXDevice, Dimension screenSize)
		{
			Font = new Font();
			var result = Font.Initialise(directXDevice.Device, "font01.txt", "font01.bmp", 32.0f, 3);

			FpsString = new Text();
			result &= FpsString.Initialise(directXDevice.Device, Font, screenSize, 16, "FPS: 0", new Coordinate2D<int>(10, 50), new Colour(NamedColour.Green), false, directXDevice.DeviceContext);

			PreviousFPS = -1;

			PositionStrings = new Text[6];
			for (int i = 0; i < PositionStrings.Length; ++i)
			{
				PositionStrings[i] = new Text();
			}

			result &= PositionStrings[0].Initialise(directXDevice.Device, Font, screenSize, 16, "X: 0", new Coordinate2D<int>(10, 90), new Colour(NamedColour.White), false, directXDevice.DeviceContext);
			result &= PositionStrings[1].Initialise(directXDevice.Device, Font, screenSize, 16, "Y: 0", new Coordinate2D<int>(10, 110), new Colour(NamedColour.White), false, directXDevice.DeviceContext);
			result &= PositionStrings[2].Initialise(directXDevice.Device, Font, screenSize, 16, "Z: 0", new Coordinate2D<int>(10, 130), new Colour(NamedColour.White), false, directXDevice.DeviceContext);
			result &= PositionStrings[3].Initialise(directXDevice.Device, Font, screenSize, 16, "rX: 0", new Coordinate2D<int>(10, 170), new Colour(NamedColour.White), false, directXDevice.DeviceContext);
			result &= PositionStrings[4].Initialise(directXDevice.Device, Font, screenSize, 16, "rY: 0", new Coordinate2D<int>(10, 190), new Colour(NamedColour.White), false, directXDevice.DeviceContext);
			result &= PositionStrings[5].Initialise(directXDevice.Device, Font, screenSize, 16, "rZ: 0", new Coordinate2D<int>(10, 210), new Colour(NamedColour.White), false, directXDevice.DeviceContext);

			PreviousPositions = new int[6];
			VideoStrings = new Text[2];
			for (int i = 0; i < VideoStrings.Length; ++i)
			{
				VideoStrings[i] = new Text();
			}

			result &= VideoStrings[0].Initialise(directXDevice.Device, Font, screenSize, 256, directXDevice.VideoCardDescription, new Coordinate2D<int>(10, 10), new Colour(NamedColour.White), false, directXDevice.DeviceContext);
			result &= VideoStrings[1].Initialise(directXDevice.Device, Font, screenSize, 64, directXDevice.VideoCardMemory.ToString(), new Coordinate2D<int>(10, 30), new Colour(NamedColour.White), false, directXDevice.DeviceContext);

			return result;
		}

		public void Dispose()
		{
			foreach (var videoString in VideoStrings)
			{
				videoString?.Dispose();
			}

			VideoStrings = null;

			foreach (var positionString in PositionStrings)
			{
				positionString?.Dispose();
			}

			PositionStrings = null;

			PreviousPositions = null;

			FpsString?.Dispose();
			FpsString = null;

			Font.Dispose();
			Font = null;
			{

			}
		}

		public bool Frame(int fps, Coordinate3D<float> position, Coordinate3D<float> rotation, DeviceContext deviceContext)
		{
			var result = UpdateFPSString(fps, deviceContext);

			result &= UpdatePositionStrings(position, rotation, deviceContext);

			return result;
		}

		public void Render(DirectX directXDevice, ShaderManager shaderManager, Matrix worldMatrix, Matrix viewMatrix, Matrix orthoMatrix)
		{
			directXDevice.SetZBuffer(false);
			directXDevice.SetAlphaBlending(true);

			FpsString.Render(directXDevice.DeviceContext, shaderManager, worldMatrix, viewMatrix, orthoMatrix, Font.Texture.TextureResource);

			foreach (var positionString in PositionStrings)
			{
				positionString.Render(directXDevice.DeviceContext, shaderManager, worldMatrix, viewMatrix, orthoMatrix, Font.Texture.TextureResource);
			}

			foreach (var videoString in VideoStrings)
			{
				videoString.Render(directXDevice.DeviceContext, shaderManager, worldMatrix, viewMatrix, orthoMatrix, Font.Texture.TextureResource);
			}

			directXDevice.SetZBuffer(true);
			directXDevice.SetAlphaBlending(false);
		}

		private bool UpdatePositionStrings(Coordinate3D<float> position, Coordinate3D<float> rotation, DeviceContext deviceContext)
		{
			var result = true;

			if (position.X != PreviousPositions[0])
			{
				PreviousPositions[0] = (int)position.X;
				var finalString = $"X: {PreviousPositions[0]}";
				result &= PositionStrings[0].UpdateSentence(Font, finalString, new Coordinate2D<int>(10, 90), new Colour(NamedColour.White), deviceContext);
			}

			if (position.Y != PreviousPositions[1])
			{
				PreviousPositions[1] = (int)position.Y;
				var finalString = $"Y: {PreviousPositions[1]}";
				result &= PositionStrings[1].UpdateSentence(Font, finalString, new Coordinate2D<int>(10, 110), new Colour(NamedColour.White), deviceContext);
			}

			if (position.Z != PreviousPositions[2])
			{
				PreviousPositions[2] = (int)position.Z;
				var finalString = $"Z: {PreviousPositions[2]}";
				result &= PositionStrings[2].UpdateSentence(Font, finalString, new Coordinate2D<int>(10, 130), new Colour(NamedColour.White), deviceContext);
			}

			if (rotation.X != PreviousPositions[3])
			{
				PreviousPositions[3] = (int)rotation.X;
				var finalString = $"rX: {PreviousPositions[3]}";
				result &= PositionStrings[3].UpdateSentence(Font, finalString, new Coordinate2D<int>(10, 170), new Colour(NamedColour.White), deviceContext);
			}

			if (rotation.Y != PreviousPositions[4])
			{
				PreviousPositions[4] = (int)rotation.Y;
				var finalString = $"rY: {PreviousPositions[4]}";
				result &= PositionStrings[4].UpdateSentence(Font, finalString, new Coordinate2D<int>(10, 190), new Colour(NamedColour.White), deviceContext);
			}

			if (rotation.Z != PreviousPositions[5])
			{
				PreviousPositions[5] = (int)rotation.Z;
				var finalString = $"rZ: {PreviousPositions[5]}";
				result &= PositionStrings[5].UpdateSentence(Font, finalString, new Coordinate2D<int>(10, 210), new Colour(NamedColour.White), deviceContext);
			}

			return result;
		}

		private bool UpdateFPSString(int fps, DeviceContext deviceContext)
		{
			var result = true;

			var colour = new Colour(NamedColour.Black);

			if (PreviousFPS != fps)
			{
				PreviousFPS = fps;

				fps = Math.Min(99999, fps);

				var finalString = $"FPS: {fps}";

				if (fps >= 60)
				{
					colour.ChangeTo(NamedColour.Green);
				}
				else if (fps >= 30)
				{
					colour.ChangeTo(NamedColour.Yellow);
				}
				else
				{
					colour.ChangeTo(NamedColour.Red);
				}

				result &= FpsString.UpdateSentence(Font, finalString, new Coordinate2D<int>(10, 50), colour, deviceContext);
			}

			return result;
		}
	}
}

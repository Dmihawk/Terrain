using SharpDX;
using SharpDX.Direct3D11;
using System;
using Visualiser.Graphics;
using Visualiser.Shaders.Font;

namespace Visualiser.Shaders
{
	public class ShaderManager : IDisposable
	{
		public TextureShader TextureShader { get; set; }
		public FontShader FontShader { get; set; }
		public FoliageShader FoliageShader { get; set; }
		public TerrainShader TerrainShader { get; set; }
		public SkyDomeShader SkyDomeShader { get; set; }
		public SkyPlaneShader SkyPlaneShader { get; set; }

		public bool Initialise(DirectX directXDevice, IntPtr windowHandle)
		{
			TextureShader = new TextureShader();
			var result = TextureShader.Initialise(directXDevice.Device, windowHandle);

			FontShader = new FontShader();
			result &= FontShader.Initialise(directXDevice.Device, windowHandle);

			FoliageShader = new FoliageShader();
			result &= FoliageShader.Initialise(directXDevice.Device, windowHandle);

			TerrainShader = new TerrainShader();
			result &= TerrainShader.Initialise(directXDevice.Device, windowHandle);

			SkyDomeShader = new SkyDomeShader();
			result &= SkyDomeShader.Initialize(directXDevice.Device, windowHandle);

			SkyPlaneShader = new SkyPlaneShader();
			result &= SkyPlaneShader.Initialize(directXDevice.Device, windowHandle);

			return result;
		}

		public void Dispose()
		{
			FoliageShader?.Dispose();
			FoliageShader = null;

			FontShader?.Dispose();
			FontShader = null;

			TextureShader?.Dispose();
			TextureShader = null;

			TerrainShader?.Dispose();
			TerrainShader = null;

			SkyDomeShader?.ShutDown();
			SkyDomeShader = null;

			SkyPlaneShader?.ShutDown();
			SkyPlaneShader = null;
		}

		public bool RenderTextureShader(DeviceContext deviceContext, int indexCount, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, ShaderResourceView texture)
		{
			var result = TextureShader.Render(deviceContext, indexCount, worldMatrix, viewMatrix, projectionMatrix, texture);

			return result;
		}

		public bool RenderFontShader(DeviceContext deviceContext, int indexCount, Matrix worldMatrix, Matrix viewMatrix, Matrix orthoMatrix, ShaderResourceView texture, Vector4 fontColour)
		{
			var result = FontShader.Render(deviceContext, indexCount, worldMatrix, viewMatrix, orthoMatrix, texture, fontColour);

			return result;
		}

		public bool RenderFoliageShader(DeviceContext deviceContext, int vertexCount, int instanceCount, Matrix viewMatrix, Matrix projectionMatrix, ShaderResourceView texture)
		{
			var result = FoliageShader.Render(deviceContext, vertexCount, instanceCount, viewMatrix, projectionMatrix, texture);

			return result;
		}

		public bool RenderTerrainShader(DeviceContext deviceContext, int indexCount, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, Color4 ambientColour, Color4 diffuseColour, Vector3 lightDirection, ShaderResourceView texture)
		{
			var result = TerrainShader.Render(deviceContext, indexCount, worldMatrix, viewMatrix, projectionMatrix, ambientColour, diffuseColour, lightDirection, texture);

			return result;
		}

		public bool RenderSkyDomeShader(DeviceContext deviceContext, int indexCount, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, Vector4 apexColour, Vector4 centerColor)
		{
			var result = SkyDomeShader.Render(deviceContext, indexCount, worldMatrix, viewMatrix, projectionMatrix, apexColour, centerColor);

			return result;
		}

		public bool RenderSkyPlaneShader(DeviceContext deviceContext, int indexCount, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, ShaderResourceView cloudTexture, ShaderResourceView perturbTexture, float translation, float scale, float brightness)
		{
			var result = SkyPlaneShader.Render(deviceContext, indexCount, worldMatrix, viewMatrix, projectionMatrix, cloudTexture, perturbTexture, translation, scale, brightness);

			return result;
		}
	}
}

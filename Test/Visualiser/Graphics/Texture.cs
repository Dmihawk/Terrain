using System;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.WIC;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Logging;
using Device = SharpDX.Direct3D11.Device;

namespace Visualiser.Graphics
{
	public class Texture : IDisposable
	{
		public ShaderResourceView TextureResource { get; private set; }

		public bool Initialise(Device device, string fileName)
		{
			try
			{
				using (var texture = LoadFromFile(device, new ImagingFactory(), fileName))
				{
					var shaderResourceViewDescription = new ShaderResourceViewDescription()
					{
						Format = texture.Description.Format,
						Dimension = ShaderResourceViewDimension.Texture2D
					};
					shaderResourceViewDescription.Texture2D.MostDetailedMip = 0;
					shaderResourceViewDescription.Texture2D.MipLevels = -1;

					TextureResource = new ShaderResourceView(device, texture, shaderResourceViewDescription);
					device.ImmediateContext.GenerateMips(TextureResource);
				}

				return true;
			}
			catch (Exception ex)
			{
				Log.WriteToFile(ErrorLevel.Error, "Texture.Initialise", ex, true);

				return false;
			}
		}

		public void Dispose()
		{
			TextureResource?.Dispose();
			TextureResource = null;
		}

		private Texture2D LoadFromFile(Device device, ImagingFactory factory, string fileName)
		{
			Texture2D texture;

			using (var bitmapSource = LoadBitmap(factory, fileName))
			{
				texture = CreateTexture2DFromBitmap(device, bitmapSource);
			}

			return texture;
		}

		private BitmapSource LoadBitmap(ImagingFactory factory, string fileName)
		{
			var bitmapDecoder = new BitmapDecoder(factory, fileName, DecodeOptions.CacheOnDemand);

			var result = new FormatConverter(factory);
			result.Initialize(bitmapDecoder.GetFrame(0), PixelFormat.Format32bppPRGBA, BitmapDitherType.None, null, 0.0D, BitmapPaletteType.Custom);

			return result;
		}

		private Texture2D CreateTexture2DFromBitmap(Device device, BitmapSource bitmapSource)
		{
			Texture2D texture;

			var stride = bitmapSource.Size.Width * 4;

			using (var buffer = new DataStream(bitmapSource.Size.Height * stride, true, true))
			{
				bitmapSource.CopyPixels(stride, buffer);

				var textureDescription = new Texture2DDescription()
				{
					Width = bitmapSource.Size.Width,
					Height = bitmapSource.Size.Height,
					ArraySize = 1,
					BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
					Usage = ResourceUsage.Default,
					CpuAccessFlags = CpuAccessFlags.None,
					Format = Format.R8G8B8A8_UNorm,
					MipLevels = 1,
					OptionFlags = ResourceOptionFlags.GenerateMipMaps,
					SampleDescription = new SampleDescription(1, 0)
				};

				texture = new Texture2D(device, textureDescription, new DataRectangle(buffer.DataPointer, stride));
			}

			return texture;
		}
	}
}
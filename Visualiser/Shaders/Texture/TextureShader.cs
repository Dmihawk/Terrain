using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using Visualiser.Containers;
using Visualiser.Containers.Buffers;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Visualiser.Shaders
{
	public class TextureShader : IDisposable
	{
		public VertexShader VertexShader { get; set; }
		public PixelShader PixelShader { get; set; }
		public InputLayout Layout { get; set; }
		public Buffer ConstantMatrixBuffer { get; set; }
		public SamplerState SamplerState { get; set; }

		public bool Initialise(Device device, IntPtr windowHandle)
		{
			return InitialiseShader(device, windowHandle, "texture.vs", "texture.ps");
		}

		private bool InitialiseShader(Device device, IntPtr windowsHandler, string vsFileName, string psFileName)
		{
			try
			{
				vsFileName = SystemConfiguration.ShaderFilePath + vsFileName;
				psFileName = SystemConfiguration.ShaderFilePath + psFileName;

				var vertexShaderByteCode = ShaderBytecode.CompileFromFile(vsFileName, "TextureVertexShader", "vs_4_0", ShaderFlags.None, EffectFlags.None);
				var pixelShaderByteCode = ShaderBytecode.CompileFromFile(psFileName, "TexturePixelShader", "ps_4_0", ShaderFlags.None, EffectFlags.None);

				VertexShader = new VertexShader(device, vertexShaderByteCode);
				PixelShader = new PixelShader(device, pixelShaderByteCode);

				var inputElements = new InputElement[]
				{
					new InputElement()
					{
						SemanticName = "POSITION",
						SemanticIndex = 0,
						Format = SharpDX.DXGI.Format.R32G32B32_Float,
						Slot = 0,
						AlignedByteOffset = 0,
						Classification = InputClassification.PerVertexData,
						InstanceDataStepRate = 0
					},
					new InputElement()
					{
						SemanticName = "TEXCOORD",
						SemanticIndex = 0,
						Format = SharpDX.DXGI.Format.R32G32_Float,
						Slot = 0,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerVertexData,
						InstanceDataStepRate = 0
					}
				};

				Layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), inputElements);

				vertexShaderByteCode.Dispose();
				pixelShaderByteCode.Dispose();

				BufferDescription matrixBufferDescription = new BufferDescription()
				{
					Usage = ResourceUsage.Dynamic,
					SizeInBytes = SharpDX.Utilities.SizeOf<WorldViewProjectionMatrixBuffer>(),
					BindFlags = BindFlags.ConstantBuffer,
					CpuAccessFlags = CpuAccessFlags.Write,
					OptionFlags = ResourceOptionFlags.None,
					StructureByteStride = 0
				};

				ConstantMatrixBuffer = new SharpDX.Direct3D11.Buffer(device, matrixBufferDescription);

				var samplerDesc = new SamplerStateDescription()
				{
					Filter = Filter.MinMagMipLinear,
					AddressU = TextureAddressMode.Wrap,
					AddressV = TextureAddressMode.Wrap,
					AddressW = TextureAddressMode.Wrap,
					MipLodBias = 0,
					MaximumAnisotropy = 1,
					ComparisonFunction = Comparison.Always,
					BorderColor = new Color4(0, 0, 0, 0),  // Black Border.
					MinimumLod = 0,
					MaximumLod = float.MaxValue
				};

				SamplerState = new SamplerState(device, samplerDesc);

				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}
		public void Dispose()
		{
			ShuddownShader();
		}
		private void ShuddownShader()
		{
			SamplerState?.Dispose();
			SamplerState = null;

			ConstantMatrixBuffer?.Dispose();
			ConstantMatrixBuffer = null;

			Layout?.Dispose();
			Layout = null;

			PixelShader?.Dispose();
			PixelShader = null;

			VertexShader?.Dispose();
			VertexShader = null;
		}
		public bool Render(DeviceContext deviceContext, int indexCount, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, ShaderResourceView texture)
		{
			if (!SetShaderParameters(deviceContext, worldMatrix, viewMatrix, projectionMatrix, texture))
				return false;

			RenderShader(deviceContext, indexCount);

			return true;
		}
		private bool SetShaderParameters(DeviceContext deviceContext, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, ShaderResourceView texture)
		{
			try
			{
				worldMatrix.Transpose();
				viewMatrix.Transpose();
				projectionMatrix.Transpose();

				deviceContext.MapSubresource(ConstantMatrixBuffer, MapMode.WriteDiscard, MapFlags.None, out DataStream mappedResource);

				var matrixBuffer = new WorldViewProjectionMatrixBuffer()
				{
					world = worldMatrix,
					view = viewMatrix,
					projection = projectionMatrix
				};
				mappedResource.Write(matrixBuffer);

				deviceContext.UnmapSubresource(ConstantMatrixBuffer, 0);

				int bufferPositionNumber = 0;

				deviceContext.VertexShader.SetConstantBuffer(bufferPositionNumber, ConstantMatrixBuffer);

				deviceContext.PixelShader.SetShaderResources(0, texture);

				return true;
			}
			catch
			{
				return false;
			}
		}
		private void RenderShader(DeviceContext deviceContext, int indexCount)
		{
			deviceContext.InputAssembler.InputLayout = Layout;

			deviceContext.VertexShader.Set(VertexShader);
			deviceContext.PixelShader.Set(PixelShader);

			deviceContext.PixelShader.SetSampler(0, SamplerState);

			deviceContext.DrawIndexed(indexCount, 0, 0);
		}
	}
}

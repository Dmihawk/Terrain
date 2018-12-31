using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using Visualiser.Containers;
using Visualiser.Containers.Buffers;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Visualiser.Shaders
{
	public class FoliageShader : IDisposable
	{
		public VertexShader VertexShader { get; set; }
		public PixelShader PixelShader { get; set; }
		public InputLayout Layout { get; set; }
		public Buffer ConstantMatrixBuffer { get; set; }
		public SamplerState SampleState { get; set; }

		public bool Initialise(Device device, IntPtr windowHandle)
		{
			return InitialiseShader(device, windowHandle, "foliage.vs", "foliage.ps");
		}

		private bool InitialiseShader(Device device, IntPtr windowHandle, string vertexShaderFileName, string pixelShaderFileName)
		{
			try
			{
				vertexShaderFileName = SystemConfiguration.ShaderFilePath + vertexShaderFileName;
				pixelShaderFileName = SystemConfiguration.ShaderFilePath + pixelShaderFileName;

				var vertexShaderByteCode = ShaderBytecode.CompileFromFile(vertexShaderFileName, "FoliageVertexShader", SystemConfiguration.VertexShaderProfile, ShaderFlags.None, EffectFlags.None);
				var pixelShaderByteCode = ShaderBytecode.CompileFromFile(pixelShaderFileName, "FoliagePixelShader", SystemConfiguration.PixelShaderProfile, ShaderFlags.None, EffectFlags.None);

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
					},
					new InputElement()
					{
						SemanticName = "WORLD",
						SemanticIndex = 0,
						Format = SharpDX.DXGI.Format.R32G32B32A32_Float,
						Slot = 1,
						AlignedByteOffset = 0,
						Classification = InputClassification.PerInstanceData,
						InstanceDataStepRate = 1
					},
					new InputElement()
					{
						SemanticName = "WORLD",
						SemanticIndex = 1,
						Format = SharpDX.DXGI.Format.R32G32B32A32_Float,
						Slot = 1,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerInstanceData,
						InstanceDataStepRate = 1
					},
					 new InputElement()
					{
						SemanticName = "WORLD",
						SemanticIndex = 2,
						Format = SharpDX.DXGI.Format.R32G32B32A32_Float,
						Slot = 1,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerInstanceData,
						InstanceDataStepRate = 1
					},
					new InputElement()
					{
						SemanticName = "WORLD",
						SemanticIndex = 3,
						Format = SharpDX.DXGI.Format.R32G32B32A32_Float,
						Slot = 1,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerInstanceData,
						InstanceDataStepRate = 1
					},
					new InputElement()
					{
						SemanticName = "TEXCOORD",
						SemanticIndex = 1,
						Format = SharpDX.DXGI.Format.R32G32B32_Float,
						Slot = 1,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerInstanceData,
						InstanceDataStepRate = 1
					}
				};

				Layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), inputElements);

				vertexShaderByteCode.Dispose();
				pixelShaderByteCode.Dispose();

				var samplerDesc = new SamplerStateDescription()
				{
					Filter = Filter.MinMagMipLinear,
					AddressU = TextureAddressMode.Wrap,
					AddressV = TextureAddressMode.Wrap,
					AddressW = TextureAddressMode.Wrap,
					MipLodBias = 0.0f,
					MaximumAnisotropy = 1,
					ComparisonFunction = Comparison.Always,
					BorderColor = new Color4(0, 0, 0, 0),
					MinimumLod = 0,
					MaximumLod = float.MaxValue
				};

				SampleState = new SamplerState(device, samplerDesc);

				BufferDescription matrixBufferDesc = new BufferDescription()
				{
					Usage = ResourceUsage.Dynamic,
					SizeInBytes = SharpDX.Utilities.SizeOf<ViewProjectionMatrixBuffer>(),
					BindFlags = BindFlags.ConstantBuffer,
					CpuAccessFlags = CpuAccessFlags.Write,
					OptionFlags = ResourceOptionFlags.None,
					StructureByteStride = 0
				};

				ConstantMatrixBuffer = new Buffer(device, matrixBufferDesc);

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
			ConstantMatrixBuffer?.Dispose();
			ConstantMatrixBuffer = null;

			SampleState?.Dispose();
			SampleState = null;

			Layout?.Dispose();
			Layout = null;

			PixelShader?.Dispose();
			PixelShader = null;

			VertexShader?.Dispose();
			VertexShader = null;
		}
		public bool Render(DeviceContext deviceContext, int vertexCount, int instanceCount, Matrix viewMatrix, Matrix projectionMatrix, ShaderResourceView texture)
		{
			if (!SetShaderParameters(deviceContext, viewMatrix, projectionMatrix, texture))
				return false;

			RenderShader(deviceContext, vertexCount, instanceCount);

			return true;
		}
		private bool SetShaderParameters(DeviceContext deviceContext, Matrix viewMatrix, Matrix projectionMatrix, ShaderResourceView texture)
		{
			try
			{
				viewMatrix.Transpose();
				projectionMatrix.Transpose();

				deviceContext.MapSubresource(ConstantMatrixBuffer, MapMode.WriteDiscard, MapFlags.None, out DataStream mappedResource);

				var matrixBuffer = new ViewProjectionMatrixBuffer()
				{
					view = viewMatrix,
					projection = projectionMatrix
				};
				mappedResource.Write(matrixBuffer);

				deviceContext.UnmapSubresource(ConstantMatrixBuffer, 0);

				int bufferPositionNumber = 0;

				deviceContext.VertexShader.SetConstantBuffer(bufferPositionNumber, ConstantMatrixBuffer);

				deviceContext.PixelShader.SetShaderResources(0, 1, texture);

				return true;
			}
			catch
			{
				return false;
			}
		}
		private void RenderShader(DeviceContext deviceContext, int vertexCount, int instanceCount)
		{
			deviceContext.InputAssembler.InputLayout = Layout;

			deviceContext.VertexShader.Set(VertexShader);
			deviceContext.PixelShader.Set(PixelShader);

			deviceContext.PixelShader.SetSampler(0, SampleState);

			deviceContext.DrawInstanced(vertexCount, instanceCount, 0, 0);
		}
	}
}
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using Visualiser.Containers;
using Visualiser.Containers.Buffers;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Visualiser.Shaders
{
	public class LightShader : IDisposable
	{
		public LightShader()
		{

		}

		public VertexShader VertexShader { get; set; }
		public PixelShader PixelShader { get; set; }
		public InputLayout Layout { get; set; }
		public Buffer ConstantMatrixBuffer { get; set; }
		public Buffer ConstantLightBuffer { get; set; }
		public SamplerState SamplerState { get; set; }

		public bool Initialise(Device device, IntPtr windowHandle)
		{
			var result = InitialiseShader(device, windowHandle, "light.vs", "light.ps");

			return result;
		}

		public void Dispose()
		{
			ConstantLightBuffer?.Dispose();
			ConstantLightBuffer = null;

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

		public bool Render(DeviceContext deviceContext, int indexCount, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, ShaderResourceView texture, Vector3 lightDirection, Vector4 diffuseColour)
		{
			var result = SetShaderParameters(deviceContext, worldMatrix, viewMatrix, projectionMatrix, texture, lightDirection, diffuseColour);

			if (result)
			{
				RenderShader(deviceContext, indexCount);
			}

			return result;
		}

		private bool InitialiseShader(Device device, IntPtr windowHandle, string vertexShaderFileName, string pixelShaderFileName)
		{
			try
			{
				SetLayout(device, vertexShaderFileName, pixelShaderFileName);
				SetSamplerState(device);
				SetBuffers(device);

				return true;
			}
			catch (Exception ex)
			{
				//Log.WriteToFile(ErrorLevel.Error, "LightShader.InitialiseShader", ex, true);

				return false;
			}
		}

		private void SetLayout(Device device, string vertexShaderFileName, string pixelShaderFileName)
		{
			vertexShaderFileName = SystemConfiguration.ShaderFilePath + vertexShaderFileName;
			pixelShaderFileName = SystemConfiguration.ShaderFilePath + pixelShaderFileName;

			using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(vertexShaderFileName, "LightVertexShader", "vs_4_0", ShaderFlags.None, EffectFlags.None))
			using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(pixelShaderFileName, "LightPixelShader", "ps_4_0", ShaderFlags.None, EffectFlags.None))
			{
				VertexShader = new VertexShader(device, vertexShaderByteCode);
				PixelShader = new PixelShader(device, pixelShaderByteCode);

				var inputElements = new InputElement[]
				{
					new InputElement()
					{
						SemanticName = "POSITION",
						SemanticIndex = 0,
						Format = Format.R32G32B32_Float,
						Slot = 0,
						AlignedByteOffset = 0,
						Classification = InputClassification.PerVertexData,
						InstanceDataStepRate = 0
					},
					new InputElement()
					{
						SemanticName = "TEXCOORD",
						SemanticIndex = 0,
						Format = Format.R32G32_Float,
						Slot = 0,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerVertexData,
						InstanceDataStepRate = 0
					},
					new InputElement()
					{
						SemanticName = "NORMAL",
						SemanticIndex = 0,
						Format = Format.R32G32B32_Float,
						Slot = 0,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerVertexData,
						InstanceDataStepRate = 0
					}
				};

				Layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), inputElements);
			}
		}

		private void SetSamplerState(Device device)
		{
			var samplerDescription = new SamplerStateDescription()
			{
				Filter = Filter.MinMagMipLinear,
				AddressU = TextureAddressMode.Wrap,
				AddressV = TextureAddressMode.Wrap,
				AddressW = TextureAddressMode.Wrap,
				MipLodBias = 0,
				MaximumAnisotropy = 1,
				ComparisonFunction = Comparison.Always,
				BorderColor = new Color4(0, 0, 0, 0),
				MinimumLod = 0,
				MaximumLod = float.MaxValue
			};

			SamplerState = new SamplerState(device, samplerDescription);
		}

		private void SetBuffers(Device device)
		{
			var matrixBufferDescription = new BufferDescription()
			{
				Usage = ResourceUsage.Dynamic,
				SizeInBytes = SharpDX.Utilities.SizeOf<WorldViewProjectionMatrixBuffer>(),
				BindFlags = BindFlags.ConstantBuffer,
				CpuAccessFlags = CpuAccessFlags.Write,
				OptionFlags = ResourceOptionFlags.None,
				StructureByteStride = 0
			};

			ConstantMatrixBuffer = new Buffer(device, matrixBufferDescription);

			var lightBufferDescription = new BufferDescription()
			{
				Usage = ResourceUsage.Dynamic,
				SizeInBytes = SharpDX.Utilities.SizeOf<LightBuffer>(),
				BindFlags = BindFlags.ConstantBuffer,
				CpuAccessFlags = CpuAccessFlags.Write,
				OptionFlags = ResourceOptionFlags.None,
				StructureByteStride = 0
			};

			ConstantLightBuffer = new Buffer(device, lightBufferDescription);
		}

		private bool SetShaderParameters(DeviceContext deviceContext, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, ShaderResourceView texture, Vector3 lightDirection, Vector4 diffuseColour)
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

				var bufferPositionNumber = 0;

				deviceContext.VertexShader.SetConstantBuffer(0, ConstantMatrixBuffer);
				deviceContext.PixelShader.SetShaderResource(0, texture);

				deviceContext.MapSubresource(ConstantLightBuffer, MapMode.WriteDiscard, MapFlags.None, out DataStream mappedResourceLight);

				var lightBuffer = new LightBuffer()
				{
					diffuseColour = diffuseColour,
					lightDirection = lightDirection,
					padding = 0
				};

				mappedResourceLight.Write(lightBuffer);

				deviceContext.UnmapSubresource(ConstantLightBuffer, 0);

				bufferPositionNumber = 0;

				deviceContext.PixelShader.SetConstantBuffer(bufferPositionNumber, ConstantLightBuffer);

				return true;
			}
			catch (Exception ex)
			{
				//Log.WriteToFile(ErrorLevel.Error, "LightShader.SetShaderParameters", ex, true);

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

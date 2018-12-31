using System;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Visualiser.Containers;
using Logging;

using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Visualiser.Containers.Buffers;

namespace Visualiser.Shaders
{
	public class ColourShader : IDisposable
	{
		public ColourShader()
		{

		}

		public VertexShader VertexShader { get; set; }
		public PixelShader PixelShader { get; set; }
		public InputLayout Layout { get; set; }
		public Buffer ConstantMatrixBuffer { get; set; }

		public bool Initialise(Device device, IntPtr windowHandle)
		{
			var result = InitialiseShader(device, windowHandle, "Colour.vs", "Colour.ps");

			return result;
		}

		public void Dispose()
		{
			ConstantMatrixBuffer?.Dispose();
			ConstantMatrixBuffer = null;

			Layout?.Dispose();
			Layout = null;

			PixelShader?.Dispose();
			PixelShader = null;

			VertexShader?.Dispose();
			VertexShader = null;
		}

		public bool Render(DeviceContext deviceContext, int indexCount, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix)
		{
			var result = SetShaderParameters(deviceContext, worldMatrix, viewMatrix, projectionMatrix);

			if (result)
			{
				RenderShader(deviceContext, indexCount);
			}

			return result;
		}

		private bool InitialiseShader(Device device, IntPtr windowHandle, string vsFileName, string psFileName)
		{
			try
			{
				SetLayout(device, vsFileName, psFileName);

				var matrixBufferDescription = new BufferDescription()
				{
					Usage = ResourceUsage.Dynamic,
					SizeInBytes = SharpDX.Utilities.SizeOf<MatrixBuffer>(),
					BindFlags = BindFlags.ConstantBuffer,
					CpuAccessFlags = CpuAccessFlags.Write,
					OptionFlags = ResourceOptionFlags.None,
					StructureByteStride = 0
				};

				ConstantMatrixBuffer = new Buffer(device, matrixBufferDescription);

				return true;
			}
			catch (Exception ex)
			{
				Log.WriteToFile(ErrorLevel.Error, "ColourShader.InitialiseShader", ex, true);

				return false;
			}
		}

		private void SetLayout(Device device, string vertexShaderFileName, string pixelShaderFileName)
		{
			vertexShaderFileName = SystemConfiguration.ShaderFilepath + vertexShaderFileName;
			pixelShaderFileName = SystemConfiguration.ShaderFilepath + pixelShaderFileName;

			using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(vertexShaderFileName, "ColourVertexShader", "vs_4_0", ShaderFlags.None, EffectFlags.None))
			using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(pixelShaderFileName, "ColourPixelShader", "ps_4_0", ShaderFlags.None, EffectFlags.None))
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
						SemanticName = "COLOUR",
						SemanticIndex = 0,
						Format = Format.R32G32B32A32_Float,
						Slot = 0,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerVertexData,
						InstanceDataStepRate = 0
					}
				};

				Layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), inputElements);
			}
		}

		private bool SetShaderParameters(DeviceContext deviceContext, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix)
		{
			try
			{
				worldMatrix.Transpose();
				viewMatrix.Transpose();
				projectionMatrix.Transpose();

				deviceContext.MapSubresource(ConstantMatrixBuffer, MapMode.WriteDiscard, MapFlags.None, out DataStream mappedResource);

				var matrixBuffer = new MatrixBuffer()
				{
					world = worldMatrix,
					view = viewMatrix,
					projection = projectionMatrix
				};

				mappedResource.Write(matrixBuffer);

				deviceContext.UnmapSubresource(ConstantMatrixBuffer, 0);

				var bufferSlotNumber = 0;

				deviceContext.VertexShader.SetConstantBuffer(bufferSlotNumber, ConstantMatrixBuffer);

				return true;
			}
			catch (Exception ex)
			{
				Log.WriteToFile(ErrorLevel.Error, "ColourShader.SetShaderParameters", ex, true);

				return false;
			}
		}

		private void RenderShader(DeviceContext deviceContext, int indexCount)
		{
			deviceContext.InputAssembler.InputLayout = Layout;
			deviceContext.VertexShader.Set(VertexShader);
			deviceContext.PixelShader.Set(PixelShader);
			deviceContext.DrawIndexed(indexCount, 0, 0);
		}
	}
}
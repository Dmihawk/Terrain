using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visualiser.Containers;
using Visualiser.Containers.Buffers;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Visualiser.Shaders
{
	public class TerrainShader : IDisposable
	{
		public VertexShader VertexShader { get; set; }
		public PixelShader PixelShader { get; set; }
		public InputLayout Layout { get; set; }
		public Buffer ConstantMatrixBuffer { get; set; }
		public Buffer ConstantLightBuffer { get; set; }
		public SamplerState SampleState { get; set; }

		public bool Initialise(Device device, IntPtr windowHandler)
		{
			return InitialiseShader(device, windowHandler, "terrain.vs", "terrain.ps");
		}
		private bool InitialiseShader(Device device, IntPtr windowHandler, string vsFileName, string psFileName)
		{
			try
			{
				// Setup full pathes
				vsFileName = SystemConfiguration.ShaderFilePath + vsFileName;
				psFileName = SystemConfiguration.ShaderFilePath + psFileName;

				// Compile the Vertex & Pixel Shader code.
				var vertexShaderByteCode = ShaderBytecode.CompileFromFile(vsFileName, "TerrainVertexShader", SystemConfiguration.VertexShaderProfile, ShaderFlags.None, EffectFlags.None);
				var pixelShaderByteCode = ShaderBytecode.CompileFromFile(psFileName, "TerrainPixelShader", SystemConfiguration.PixelShaderProfile, ShaderFlags.None, EffectFlags.None);

				// Create the Vertex & Pixel Shader from the buffer.
				VertexShader = new VertexShader(device, vertexShaderByteCode);
				PixelShader = new PixelShader(device, pixelShaderByteCode);

				// Now setup the layout of the data that goes into the shader.
				// This setup needs to match the VertexType structure in the Model and in the shader.
				InputElement[] inputElements = new InputElement[]
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
						SemanticName = "NORMAL",
						SemanticIndex = 0,
						Format = SharpDX.DXGI.Format.R32G32B32_Float,
						Slot = 0,
						AlignedByteOffset = InputElement.AppendAligned,
						Classification = InputClassification.PerVertexData,
						InstanceDataStepRate = 0
					}
				};

				// Create the vertex input the layout.
				Layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), inputElements);

				// Release the vertex and pixel shader buffers, since they are no longer needed.
				vertexShaderByteCode.Dispose();
				pixelShaderByteCode.Dispose();

				// Create a texture sampler state description.
				SamplerStateDescription samplerDesc = new SamplerStateDescription()
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

				// Create the texture sampler state.
				SampleState = new SamplerState(device, samplerDesc);

				// Setup the description of the dynamic matrix constant buffer that is in the vertex shader.
				BufferDescription matrixBufferDesc = new BufferDescription()
				{
					Usage = ResourceUsage.Dynamic,
					SizeInBytes = SharpDX.Utilities.SizeOf<WorldViewProjectionMatrixBuffer>(),
					BindFlags = BindFlags.ConstantBuffer,
					CpuAccessFlags = CpuAccessFlags.Write,
					OptionFlags = ResourceOptionFlags.None,
					StructureByteStride = 0
				};

				// Create the constant buffer pointer so we can access the vertex shader constant buffer from within this class.
				ConstantMatrixBuffer = new Buffer(device, matrixBufferDesc);

				// Setup the description of the light dynamic constant bufffer that is in the pixel shader.
				// Note that ByteWidth alwalys needs to be a multiple of the 16 if using D3D11_BIND_CONSTANT_BUFFER or CreateBuffer will fail.
				BufferDescription lightBufferDesc = new BufferDescription()
				{
					Usage = ResourceUsage.Dynamic,
					SizeInBytes = SharpDX.Utilities.SizeOf<LightBuffer>(), // Must be divisable by 16 bytes, so this is equated to 32.
					BindFlags = BindFlags.ConstantBuffer,
					CpuAccessFlags = CpuAccessFlags.Write,
					OptionFlags = ResourceOptionFlags.None,
					StructureByteStride = 0
				};

				// Create the constant buffer pointer so we can access the vertex shader constant buffer from within this class.
				ConstantLightBuffer = new Buffer(device, lightBufferDesc);

				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}
		public void Dispose()
		{
			// Shutdown the vertex and pixel shaders as well as the related objects.
			ShuddownShader();
		}
		private void ShuddownShader()
		{
			// Release the light constant buffer.
			ConstantLightBuffer?.Dispose();
			ConstantLightBuffer = null;
			// Release the matrix constant buffer.
			ConstantMatrixBuffer?.Dispose();
			ConstantMatrixBuffer = null;
			// Release the sampler state.
			SampleState?.Dispose();
			SampleState = null;
			// Release the layout.
			Layout?.Dispose();
			Layout = null;
			// Release the pixel shader.
			PixelShader?.Dispose();
			PixelShader = null;
			// Release the vertex shader.
			VertexShader?.Dispose();
			VertexShader = null;
		}
		public bool Render(DeviceContext deviceContext, int indexCount, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, Vector4 ambientColour, Vector4 diffuseColour, Vector3 lightDirection, ShaderResourceView texture)
		{
			// Set the shader parameters that it will use for rendering.
			if (!SetShaderParameters(deviceContext, worldMatrix, viewMatrix, projectionMatrix, ambientColour, diffuseColour, lightDirection, texture))
				return false;

			// Now render the prepared buffers with the shader.
			RenderShader(deviceContext, indexCount);

			return true;
		}
		private bool SetShaderParameters(DeviceContext deviceContext, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, Vector4 ambientColour, Vector4 diffuseColour, Vector3 lightDirection, ShaderResourceView texture)
		{
			try
			{
				// Transpose the matrices to prepare them for shader.
				worldMatrix.Transpose();
				viewMatrix.Transpose();
				projectionMatrix.Transpose();

				// Lock the matrix constant buffer so it can be written to.
				DataStream mappedResource;
				deviceContext.MapSubresource(ConstantMatrixBuffer, MapMode.WriteDiscard, MapFlags.None, out mappedResource);

				// Copy the matrices into the constant buffer.
				var matrixBuffer = new WorldViewProjectionMatrixBuffer()
				{
					world = worldMatrix,
					view = viewMatrix,
					projection = projectionMatrix
				};
				mappedResource.Write(matrixBuffer);

				// Unlock the constant buffer.
				deviceContext.UnmapSubresource(ConstantMatrixBuffer, 0);

				// Set the position of the constant buffer in the vertex shader.
				int bufferPositionNumber = 0;

				// Finally set the constant buffer in the vertex shader with the updated values.
				deviceContext.VertexShader.SetConstantBuffer(bufferPositionNumber, ConstantMatrixBuffer);

				// Set shader resource in the pixel shader.
				deviceContext.PixelShader.SetShaderResource(0, texture);

				// Lock the light constant buffer so it can be written to.
				deviceContext.MapSubresource(ConstantLightBuffer, MapMode.WriteDiscard, MapFlags.None, out mappedResource);

				// Copy the lighting variables into the constant buffer.
				var lightBuffer = new LightBuffer()
				{
					ambientColour = ambientColour,
					diffuseColour = diffuseColour,
					lightDirection = lightDirection,
					padding = 0
				};
				mappedResource.Write(lightBuffer);

				// Unlock the constant buffer.
				deviceContext.UnmapSubresource(ConstantLightBuffer, 0);

				// Set the position of the light constant buffer in the pixel shader.
				bufferPositionNumber = 0;

				// Finally set the light constant buffer in the pixel shader with the updated values.
				deviceContext.PixelShader.SetConstantBuffer(bufferPositionNumber, ConstantLightBuffer);

				return true;
			}
			catch
			{
				return false;
			}
		}
		private void RenderShader(DeviceContext deviceContext, int indexCount)
		{
			// Set the vertex input layout.
			deviceContext.InputAssembler.InputLayout = Layout;

			// Set the vertex and pixel shaders that will be used to render this triangle.
			deviceContext.VertexShader.Set(VertexShader);
			deviceContext.PixelShader.Set(PixelShader);

			// Set the sampler state in the pixel shader.
			deviceContext.PixelShader.SetSampler(0, SampleState);

			// Render the triangle.
			deviceContext.DrawIndexed(indexCount, 0, 0);
		}
	}
}

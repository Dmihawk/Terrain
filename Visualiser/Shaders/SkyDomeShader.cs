using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Visualiser.Containers;

namespace Visualiser.Shaders
{
	public class SkyDomeShader
	{
		//Structs
		[StructLayout(LayoutKind.Sequential)]
		public struct DMatrixBuffer
		{
			public Matrix world;
			public Matrix view;
			public Matrix projection;
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct DGradientBuffer
		{
			public Vector4 apexColor;
			public Vector4 centerColor;
		}

		// Properties
		public VertexShader VertexShader { get; set; }
		public PixelShader PixelShader { get; set; }
		public InputLayout Layout { get; set; }
		public SharpDX.Direct3D11.Buffer ConstantMatrixBuffer { get; set; }
		public SharpDX.Direct3D11.Buffer ConstantGradientBuffer { get; set; }

		// Methods
		public bool Initialize(Device device, IntPtr hwnd)
		{
			// Initialize the vertex and pixel shaders.
			if (!InitializeShader(device, hwnd, "skydome.vs", "skydome.ps"))
				return false;

			return true;
		}
		private bool InitializeShader(Device device, IntPtr hwnd, string vsFileName, string psFileName)
		{
			try
			{
				// Setup full pathes
				vsFileName = SystemConfiguration.ShaderFilePath + vsFileName;
				psFileName = SystemConfiguration.ShaderFilePath + psFileName;

				// Compile the Vertex & Pixel Shader code.
				ShaderBytecode vertexShaderByteCode = ShaderBytecode.CompileFromFile(vsFileName, "SkyDomeVertexShader", SystemConfiguration.VertexShaderProfile, ShaderFlags.None, EffectFlags.None);
				ShaderBytecode pixelShaderByteCode = ShaderBytecode.CompileFromFile(psFileName, "SkyDomePixelShader", SystemConfiguration.PixelShaderProfile, ShaderFlags.None, EffectFlags.None);

				// Create the Vertex & Pixel Shader from the buffer.
				VertexShader = new VertexShader(device, vertexShaderByteCode);
				PixelShader = new PixelShader(device, pixelShaderByteCode);

				// Create the vertex input layout description.
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
					}
				};

				// Create the vertex input the layout.
				Layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), inputElements);

				// Release the vertex and pixel shader buffers, since they are no longer needed.
				vertexShaderByteCode.Dispose();
				pixelShaderByteCode.Dispose();

				// Setup the description of the dynamic matrix constant buffer that is in the vertex shader.
				BufferDescription matrixBufferDesc = new BufferDescription()
				{
					Usage = ResourceUsage.Dynamic,
					SizeInBytes = SharpDX.Utilities.SizeOf<DMatrixBuffer>(),
					BindFlags = BindFlags.ConstantBuffer,
					CpuAccessFlags = CpuAccessFlags.Write,
					OptionFlags = ResourceOptionFlags.None,
					StructureByteStride = 0
				};

				// Create the constant buffer pointer so we can access the vertex shader constant buffer from within this class.
				ConstantMatrixBuffer = new SharpDX.Direct3D11.Buffer(device, matrixBufferDesc);

				// Setup the description of the gradient constant buffer that is in the pixel shader.
				BufferDescription gradientBufferDesc = new BufferDescription()
				{
					Usage = ResourceUsage.Dynamic,
					SizeInBytes = SharpDX.Utilities.SizeOf<DGradientBuffer>(),
					BindFlags = BindFlags.ConstantBuffer,
					CpuAccessFlags = CpuAccessFlags.Write,
					OptionFlags = ResourceOptionFlags.None,
					StructureByteStride = 0
				};

				// Create the constant buffer pointer so we can access the pixel shader constant buffer from within this class.
				ConstantGradientBuffer = new SharpDX.Direct3D11.Buffer(device, gradientBufferDesc);

				return true;
			}
			catch
			{

				return false;
			}
		}
		public void ShutDown()
		{
			// Shutdown the vertex and pixel shaders as well as the related objects.
			ShutdownShader();
		}
		private void ShutdownShader()
		{
			// Release the gradient constant buffer.
			ConstantGradientBuffer?.Dispose();
			ConstantGradientBuffer = null;
			// Release the matrix constant buffer.
			ConstantMatrixBuffer?.Dispose();
			ConstantMatrixBuffer = null;
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
		public bool Render(DeviceContext deviceContext, int indexCount, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, Vector4 apexColour, Vector4 centerColor)
		{
			// Set the shader parameters that it will use for rendering.
			if (!SetShaderParameters(deviceContext, worldMatrix, viewMatrix, projectionMatrix, apexColour, centerColor))
				return false;

			// Now render the prepared buffers with the shader.
			RenderShader(deviceContext, indexCount);

			return true;
		}

		private void RenderShader(DeviceContext deviceContext, int indexCount)
		{
			// Set the vertex input layout.
			deviceContext.InputAssembler.InputLayout = Layout;

			// Set the vertex and pixel shaders that will be used to render this triangle.
			deviceContext.VertexShader.Set(VertexShader);
			deviceContext.PixelShader.Set(PixelShader);

			// Render the triangle.
			deviceContext.DrawIndexed(indexCount, 0, 0);
		}
		private bool SetShaderParameters(DeviceContext deviceContext, Matrix worldMatrix, Matrix viewMatrix, Matrix projectionMatrix, Vector4 apexColour, Vector4 centerColor)
		{
			// Transpose the matrices to prepare them for the shader.
			// Transpose the matrices to prepare them for shader.
			worldMatrix.Transpose();
			viewMatrix.Transpose();
			projectionMatrix.Transpose();

			// Lock the constant buffer so it can be written to.
			DataStream mappedResource;
			deviceContext.MapSubresource(ConstantMatrixBuffer, MapMode.WriteDiscard, MapFlags.None, out mappedResource);

			// Copy the matrices into the constant buffer.
			DMatrixBuffer matrixBuffer = new DMatrixBuffer()
			{
				world = worldMatrix,
				view = viewMatrix,
				projection = projectionMatrix
			};
			mappedResource.Write(matrixBuffer);

			// Unlock the constant buffer.
			deviceContext.UnmapSubresource(ConstantMatrixBuffer, 0);

			// Set the position of the constant buffer in the vertex shader.
			int bufferNumber = 0;

			// Finally set the constant buffer in the vertex shader with the updated values.
			deviceContext.VertexShader.SetConstantBuffer(bufferNumber, ConstantMatrixBuffer);

			// Lock the gradient constant buffer so it can be written to.
			deviceContext.MapSubresource(ConstantGradientBuffer, MapMode.WriteDiscard, MapFlags.None, out mappedResource);

			// Copy the gradient color variables into the constant buffer.
			DGradientBuffer gradientBuffer = new DGradientBuffer()
			{
				apexColor = apexColour,
				centerColor = centerColor
			};
			mappedResource.Write(gradientBuffer);

			// Unlock the constant buffer.
			deviceContext.UnmapSubresource(ConstantGradientBuffer, 0);

			// Set the position of the gradient constant buffer in the pixel shader.
			bufferNumber = 0;

			// Finally set the gradient constant buffer in the pixel shader with the updated values.
			deviceContext.PixelShader.SetConstantBuffer(bufferNumber, ConstantGradientBuffer);

			return true;
		}
	}
}

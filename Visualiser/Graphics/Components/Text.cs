using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using Visualiser.Containers;
using Visualiser.Containers.Vertices;
using Visualiser.Shaders;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Visualiser.Graphics.Components
{
	public class Text : IDisposable
	{
		private const int NumSentences = 12;

		public Dimension ScreenSize;
		public Sentence[] Sentences = new Sentence[NumSentences];

		public bool Shadowed { get; set; }
		public int MaxLength { get; set; }
		public int VertexCount { get; set; }
		public int IndexCount { get; set; }
		public Vector4 PixelColour { get; set; }
		public Buffer VertexBuffer { get; set; }
		public Buffer VertexBuffer2 { get; set; }
		public Buffer IndexBuffer { get; set; }
		public Buffer IndexBuffer2 { get; set; }

		public bool Initialise(Device device, Font font, Dimension screenSize, int maxLength, string text, Coordinate2D<int> position, Colour colour, bool shadowed, DeviceContext deviceContext)
		{
			Shadowed = shadowed;
			ScreenSize = screenSize;
			MaxLength = maxLength;

			var result = InitialiseSentence(device, font, text, position, colour, deviceContext);

			return result;
		}

		public void Dispose()
		{
			foreach (var sentence in Sentences)
			{
				ReleaseSentence(sentence);
			}

			Sentences = null;

			VertexBuffer?.Dispose();
			VertexBuffer = null;

			VertexBuffer2?.Dispose();
			VertexBuffer2 = null;

			IndexBuffer?.Dispose();
			IndexBuffer = null;

			IndexBuffer2?.Dispose();
			IndexBuffer2 = null;
		}

		public bool Render(DeviceContext deviceContext, ShaderManager shaderManager, Matrix worldMatrix, Matrix viewMatrix, Matrix orthoMatrix, ShaderResourceView fontTexture)
		{
			var result = RenderSentence(deviceContext, shaderManager, worldMatrix, viewMatrix, orthoMatrix, fontTexture);

			return result;
		}

		private void ReleaseSentence(Sentence sentence)
		{
			sentence.VertexBuffer?.Dispose();
			sentence.VertexBuffer = null;

			sentence.IndexBuffer?.Dispose();
			sentence.IndexBuffer = null;
		}

		private bool InitialiseSentence(Device device, Font font, string text, Coordinate2D<int> position, Colour colour, DeviceContext deviceContext)
		{
			VertexCount = 6 * MaxLength;
			IndexCount = 6 * MaxLength;

			var vertices = new PositionTextureVertex[VertexCount];
			var indices = new int[IndexCount];

			for (var i = 0; i < vertices.Length; ++i)
			{
				vertices[i].position = Vector3.Zero;
				vertices[i].texture = Vector2.Zero;
			}

			for (var i = 0; i < IndexCount; ++i)
			{
				indices[i] = i;
			}

			var vertexBufferDescription = new BufferDescription()
			{
				Usage = ResourceUsage.Dynamic,
				SizeInBytes = SharpDX.Utilities.SizeOf<PositionTextureVertex>() * VertexCount,
				BindFlags = BindFlags.VertexBuffer,
				CpuAccessFlags = CpuAccessFlags.Write,
				OptionFlags = ResourceOptionFlags.None,
				StructureByteStride = 0
			};

			VertexBuffer = Buffer.Create(device, vertices, vertexBufferDescription);
			IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indices);

			var result = true;

			if (Shadowed)
			{
				try
				{
					VertexBuffer2 = Buffer.Create(device, vertices, vertexBufferDescription);
					IndexBuffer2 = Buffer.Create(device, BindFlags.IndexBuffer, indices);
				}
				catch
				{
					result = false;
				}
			}

			vertices = null;
			indices = null;

			result &= UpdateSentence(font, text, position, colour, deviceContext);

			return result;
		}

		public bool UpdateSentence(Font font, string text, Coordinate2D<int> position, Colour colour, DeviceContext deviceContext)
		{
			PixelColour = new Vector4(colour.Red, colour.Green, colour.Blue, 1.0f);

			var numLetters = text.Length;

			var result = false;

			if (numLetters <= MaxLength)
			{
				var drawPosition = new Coordinate2D<float>()
				{
					X = -(ScreenSize.Width >> 1) + position.X,
					Y = (ScreenSize.Height >> 1) - position.Y
				};

				font.BuildVertexArray(out List<PositionTextureVertex> vertices, text, drawPosition);

				for (var i = numLetters; i < MaxLength; ++i)
				{
					var emptyVertex = new PositionTextureVertex()
					{
						position = Vector3.Zero,
						texture = Vector2.Zero
					};

					vertices.Add(emptyVertex);
				}

				deviceContext.MapSubresource(VertexBuffer, MapMode.WriteDiscard, MapFlags.None, out DataStream mappedResource);
				mappedResource.WriteRange(vertices.ToArray());
				deviceContext.UnmapSubresource(VertexBuffer, 0);

				// If shadowed then do the same for the second vertex buffer but offset by one pixel.
				if (Shadowed)
				{
					// Perform same mapping and writings code as above except for the shadows offset position for the effect.
				}

				vertices?.Clear();
				vertices = null;

				result = true;
			}

			return result;
		}

		private bool RenderSentence(DeviceContext deviceContext, ShaderManager shaderManager, Matrix worldMatrix, Matrix viewMatrix, Matrix orthoMatrix, ShaderResourceView fontTexture)
		{
			var stride = SharpDX.Utilities.SizeOf<PositionTextureVertex>();
			var offset = 0;

			if (Shadowed)
			{
				var shadowColour = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
				deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(VertexBuffer2, stride, offset));
				deviceContext.InputAssembler.SetIndexBuffer(IndexBuffer2, Format.R32_UInt, 0);
				deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
				shaderManager.RenderFontShader(deviceContext, IndexCount, worldMatrix, viewMatrix, orthoMatrix, fontTexture, shadowColour);
			}

			deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(VertexBuffer, stride, offset));
			deviceContext.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
			deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			shaderManager.RenderFontShader(deviceContext, IndexCount, worldMatrix, viewMatrix, orthoMatrix, fontTexture, PixelColour);

			return true;
		}
	}
}

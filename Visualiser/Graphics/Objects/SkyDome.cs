using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visualiser.Containers;
using Visualiser.Containers.Vertices;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Visualiser.Graphics.Objects
{
	public class SkyDome
	{
		public XYZTextureNormalType[] Model { get; set; }
		public int VertexCount { get; set; }
		public int IndexCount { get; set; }
		public Buffer VertexBuffer { get; set; }
		public Buffer IndexBuffer { get; set; }
		public Vector4 ApexColour { get; set; }
		public Vector4 CenterColour { get; set; }

		public bool Initialise(Device device)
		{
			// Load in the sky dome model.
			if (!LoadSkyDomeModel("skydome.txt"))
				return false;

			// Load the sky dome into a vertex and index buffer for rendering.
			if (!InitializeBuffers(device))
				return false;

			// Set the Pink color at the top of the sky dome.
			ApexColour = new Vector4(0.0f, 0.15f, 0.66f, 1.0f);

			// Set the Blue color at the center of the sky dome.
			CenterColour = new Vector4(0.81f, 0.38f, 0.66f, 1.0f);

			return true;
		}
		private bool InitializeBuffers(SharpDX.Direct3D11.Device device)
		{
			// Create the vertex array.
			var vertices = new PositionVertex[VertexCount];
			// Create the index array.
			int[] indices = new int[IndexCount];

			// Load the vertex array and index array with data.
			for (int i = 0; i < VertexCount; i++)
			{
				vertices[i].position = new Vector3(Model[i].x, Model[i].y, Model[i].z);
				indices[i] = i;
			}

			// Now finally create the vertex buffer.
			// Create the vertex buffer.
			VertexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, vertices);

			// Create the index buffer.
			IndexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.IndexBuffer, indices);

			// Release the arrays now that the buffers have been created and loaded.
			vertices = null;
			indices = null;
			ReleaseSkyDomeModel();

			return true;
		}
		private bool LoadSkyDomeModel(string skyDomeModelFileName)
		{
			skyDomeModelFileName = SystemConfiguration.ModelFilePath + skyDomeModelFileName;

			try
			{
				// Open the model file.
				string[] lines = File.ReadAllLines(skyDomeModelFileName);

				// Read up to the value of vertex count.
				int lineIndex = 0;
				bool found = false;
				while (!found)
				{
					if (lines[lineIndex].StartsWith("Vertex Count:"))
						found = true;
					else
						lineIndex++;
				}

				// Read in the vertex count, the second column after the ':' of the first row in the file.
				string stringVertexCount = lines[lineIndex].Split(':')[1];
				VertexCount = int.Parse(stringVertexCount);

				// Set the number of indices to be the same as the vertex count.
				IndexCount = VertexCount;
				// Create the model using the vertex count that was read in.
				Model = new XYZTextureNormalType[VertexCount];

				// Before continueing with the line parsing ensure we are starting one line after the "Data" portion of the file.
				int lineDataIndex = ++lineIndex;
				found = false;
				while (!found)
				{
					if (lines[lineDataIndex].Equals("Data:"))
						found = true;
					else
						lineDataIndex++;
				}

				// Procced to the next line for Vertex data.
				lineDataIndex++;

				// Read up to the beginning of the data.
				int vertexWriteIndex = 0;
				for (int i = lineDataIndex; i < lines.Length; i++)
				{
					// Ignor blank lines.
					if (string.IsNullOrEmpty(lines[i]))
						continue;

					// break out segments of this line.
					string[] segments = lines[i].Split(' ');

					//Model[vertexWriteIndex] = new DModelType()
					//{
					// Read in the vertex data, First X, Y & Z positions.
					Model[vertexWriteIndex].x = float.Parse(segments[0]);
					Model[vertexWriteIndex].y = float.Parse(segments[1], NumberStyles.Float);
					Model[vertexWriteIndex].z = float.Parse(segments[2], NumberStyles.Float);

					// Read in the Tu and Yv tecture coordinate values.
					Model[vertexWriteIndex].tu = float.Parse(segments[3], NumberStyles.Float);
					Model[vertexWriteIndex].tv = float.Parse(segments[4], NumberStyles.Float);

					// Read in the Normals X, Y & Z values.
					Model[vertexWriteIndex].nx = float.Parse(segments[5], NumberStyles.Float);
					Model[vertexWriteIndex].ny = float.Parse(segments[6], NumberStyles.Float);
					Model[vertexWriteIndex].nz = float.Parse(segments[7], NumberStyles.Float, CultureInfo.InvariantCulture);
					vertexWriteIndex++;
				}
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}
		public void ShutDown()
		{
			// Release the vertex and index buffer that were used for rendering the sky dome.
			ReleaseBuffers();

			// Release the sky dome model.
			ReleaseSkyDomeModel();
		}
		private void ReleaseBuffers()
		{
			// Release the index buffer.
			IndexBuffer?.Dispose();
			IndexBuffer = null;
			// Release the vertex buffer.
			VertexBuffer?.Dispose();
			VertexBuffer = null;
		}
		private void ReleaseSkyDomeModel()
		{
			if (Model != null)
				Model = null;
		}
		public void Render(DeviceContext deviceContext)
		{
			// Render the sky dome.
			RenderBuffers(deviceContext);
		}
		private void RenderBuffers(DeviceContext deviceContext)
		{
			// Set the vertex buffer to active in the input assembler so it can be rendered.
			deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(VertexBuffer, SharpDX.Utilities.SizeOf<PositionVertex>(), 0));

			// Set the index buffer to active in the input assembler so it can be rendered.
			deviceContext.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);

			// Set the type of the primitive that should be rendered from this vertex buffer, in this case triangles.
			deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
		}
	}
}

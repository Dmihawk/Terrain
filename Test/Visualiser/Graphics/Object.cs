using System;
using System.IO;
using System.Linq;
using Logging;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Visualiser.Containers;
using Visualiser.Containers.Vertices;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Visualiser.Graphics
{
	public class Object : IDisposable
	{
		private Buffer _vertexBuffer;
		private Buffer _indexBuffer;
		private int _vertexCount;

		public Object()
		{

		}

		public int IndexCount { get; set; }
		public Texture Texture { get; set; }
		public ObjectFormat[] ModelObject { get; private set; }

		public bool Initialise(Device device, string objectFormatFileName, string textureFileName)
		{
			var result = true;

			result &= LoadObject(objectFormatFileName);
			result &= InitialiseBuffers(device);
			result &= LoadTexture(device, textureFileName);

			return result;
		}

		public void Dispose()
		{
			Texture?.Dispose();
			Texture = null;

			_indexBuffer?.Dispose();
			_indexBuffer = null;

			_vertexBuffer?.Dispose();
			_vertexBuffer = null;

			ModelObject = null;
		}

		public void Render(DeviceContext deviceContext)
		{
			RenderBuffers(deviceContext);
		}

		private bool LoadObject(string objectFormatFileName)
		{
			objectFormatFileName = SystemConfiguration.ObjectFilePath + objectFormatFileName;

			try
			{
				var lines = File.ReadLines(objectFormatFileName).ToList();

				var vertextCountString = lines[0].Split(new char[] { ':' })[1].Trim();
				_vertexCount = int.Parse(vertextCountString);
				IndexCount = _vertexCount;
				ModelObject = new ObjectFormat[_vertexCount];

				for (var i = 4; i < lines.Count && i < 4 + _vertexCount; ++i)
				{
					var objectArray = lines[i].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

					ModelObject[i - 4] = new ObjectFormat()
					{
						x = float.Parse(objectArray[0]),
						y = float.Parse(objectArray[1]),
						z = float.Parse(objectArray[2]),
						tu = float.Parse(objectArray[3]),
						tv = float.Parse(objectArray[4]),
						nx = float.Parse(objectArray[5]),
						ny = float.Parse(objectArray[6]),
						nz = float.Parse(objectArray[7]),
					};
				}

				return true;
			}
			catch (Exception ex)
			{
				Log.WriteToFile(ErrorLevel.Error, "Object.LoadObject", ex, true);

				return false;
			}
		}

		private bool InitialiseBuffers(Device device)
		{
			try
			{
				var vertices = new PositionTextureNormalVertex[_vertexCount];
				var indices = new int[IndexCount];

				for (var i = 0; i < _vertexCount; ++i)
				{
					vertices[i] = new PositionTextureNormalVertex()
					{
						position = new Vector3(ModelObject[i].x, ModelObject[i].y, ModelObject[i].z),
						texture = new Vector2(ModelObject[i].tu, ModelObject[i].tv),
						normal = new Vector3(ModelObject[i].nx, ModelObject[i].ny, ModelObject[i].nz)
					};

					indices[i] = i;
				}

				_vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices);
				_indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indices);

				return true;
			}
			catch (Exception ex)
			{
				Log.WriteToFile(ErrorLevel.Error, "Model.InitialiseBuffer", ex, true);

				return false;
			}
		}

		private bool LoadTexture(Device device, string textureFileName)
		{
			textureFileName = SystemConfiguration.DataFilePath + textureFileName;

			Texture = new Texture();
			Texture.Initialise(device, textureFileName);

			return true;
		}

		private void RenderBuffers(DeviceContext deviceContext)
		{
			deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, SharpDX.Utilities.SizeOf<PositionTextureNormalVertex>(), 0));
			deviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);
			deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
		}
	}
}

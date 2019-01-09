using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Visualiser.Containers;
using Visualiser.Containers.Enums;
using Visualiser.Containers.Types;
using Visualiser.Containers.Vertices;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Visualiser.Graphics.Objects
{
	public class Terrain : IDisposable
	{
		private Dimension _size;
		private Buffer _vertexBuffer;
		private Buffer _indexBuffer;
		private readonly int _textureRepeat = 8;

		public Terrain()
		{
			_size = new Dimension();
			_cachedNormals = new Dictionary<Coordinate2D<int>, XYZType>();
		}

		public int IndexCount { get; private set; }
		public int VertexCount { get; private set; }
		public List<XYZTextureNormalType> HeightMap = new List<XYZTextureNormalType>();
		public Texture Texture { get; set; }
		public PositionTextureNormalVertex[] Vertices { get; set; }

		public bool Initialise(Device device, string heightMapFileName, string textureFileName)
		{
			var result = LoadHeightMap(heightMapFileName);

			NormaliseHeightMap();

			result &= CalculateNormals();

			CalculateTextureCoordinates();

			result &= LoadTexture(device, textureFileName);

			result &= InitialiseBuffers(device);

			return result;
		}

		public void Dispose()
		{
			_indexBuffer?.Dispose();
			_indexBuffer = null;

			_vertexBuffer?.Dispose();
			_vertexBuffer = null;

			HeightMap?.Clear();
			HeightMap = null;

			Texture?.Dispose();
			Texture = null;
		}

		public void Render(DeviceContext deviceContext)
		{
			RenderBuffers(deviceContext);
		}

		public void ChangeHeightAtPosition(Device device, int x, int z, float height)
		{
			var index = GetIndexFromPosition(x, z);

			ChangeHeight(index, height);

			var halfHeight = height / 2;

			ChangeHeight(GetRelativeIndex(RelativePosition.TopLeft, index), halfHeight);
			ChangeHeight(GetRelativeIndex(RelativePosition.TopMiddle, index), halfHeight);
			ChangeHeight(GetRelativeIndex(RelativePosition.TopRight, index), halfHeight);
			ChangeHeight(GetRelativeIndex(RelativePosition.Left, index), halfHeight);
			ChangeHeight(GetRelativeIndex(RelativePosition.Right, index), halfHeight);
			ChangeHeight(GetRelativeIndex(RelativePosition.BottomLeft, index), halfHeight);
			ChangeHeight(GetRelativeIndex(RelativePosition.BottomMiddle, index), halfHeight);
			ChangeHeight(GetRelativeIndex(RelativePosition.BottomRight, index), halfHeight);

			UpdateNormal(x - 1, z - 1);
			UpdateNormal(x, z - 1);
			UpdateNormal(x + 1, z - 1);

			UpdateNormal(x - 1, z);
			UpdateNormal(x, z);
			UpdateNormal(x + 1, z);

			UpdateNormal(x - 1, z + 1);
			UpdateNormal(x, z + 1);
			UpdateNormal(x + 1, z + 1);

			InitialiseBuffers(device);
		}

		private int GetIndexFromPosition(int x, int z)
		{
			return (z * _size.Width) + x;
		}

		private int GetRelativeIndex(RelativePosition position, int index)
		{
			var relativeIndex = -1;
			var width = _size.Width;

			switch (position)
			{
				case RelativePosition.TopLeft:
					relativeIndex = index - width - 1;
					break;

				case RelativePosition.TopMiddle:
					relativeIndex = index - width;
					break;

				case RelativePosition.TopRight:
					relativeIndex = index - width + 1;
					break;

				case RelativePosition.Left:
					relativeIndex = index - 1;
					break;

				case RelativePosition.Right:
					relativeIndex = index + 1;
					break;

				case RelativePosition.BottomLeft:
					relativeIndex = index + width - 1;
					break;

				case RelativePosition.BottomMiddle:
					relativeIndex = index + width;
					break;

				case RelativePosition.BottomRight:
					relativeIndex = index + width + 1;
					break;
			}

			return relativeIndex;
		}

		private void ChangeHeight(int index, float height)
		{
			var original = HeightMap[index];

			HeightMap[index] = new XYZTextureNormalType()
			{
				x = original.x,
				y = original.y + height,
				z = original.z,
				tu = original.tu,
				tv = original.tv,
				nx = original.nx,
				ny = original.ny,
				nz = original.nz
			};
		}

		private void ChangeNormal(int index, float nx, float ny, float nz)
		{
			var original = HeightMap[index];

			HeightMap[index] = new XYZTextureNormalType()
			{
				x = original.x,
				y = original.y,
				z = original.z,
				tu = original.tu,
				tv = original.tv,
				nx = nx,
				ny = ny,
				nz = nz
			};
		}

		private void NormaliseHeightMap()
		{
			for (var i = 0; i < HeightMap.Count; ++i)
			{
				var temp = HeightMap[i];
				temp.y /= 15;
				HeightMap[i] = temp;
			}
		}

		private bool LoadTexture(Device device, string textureFileName)
		{
			textureFileName = SystemConfiguration.DataFilePath + textureFileName;

			Texture = new Texture();

			var result = Texture.Initialise(device, textureFileName);

			return result;
		}

		private void CalculateTextureCoordinates()
		{
			// Calculate how much to increment the texture coordinates by.
			float incrementValue = (float)_textureRepeat / (float)_size.Width;

			// Calculate how many times to repeat the texture.
			int incrementCount = _size.Width / _textureRepeat;

			// Initialize the tu and tv coordinate values.
			float tuCoordinate = 0.0f;
			float tvCoordinate = 1.0f;

			// Initialize the tu and tv coordinate indexes.
			int tuCount = 0;
			int tvCount = 0;

			// Loop through the entire height map and calculate the tu and tv texture coordinates for each vertex.
			for (int j = 0; j < _size.Height; j++)
			{
				for (int i = 0; i < _size.Width; i++)
				{
					// Store the texture coordinate in the height map.
					var tempHeightMap = HeightMap[(_size.Height * j) + i];
					tempHeightMap.tu = tuCoordinate;
					tempHeightMap.tv = tvCoordinate;
					HeightMap[(_size.Height * j) + i] = tempHeightMap;

					// Increment the tu texture coordinate by the increment value and increment the index by one.
					tuCoordinate += incrementValue;
					tuCount++;

					// Check if at the far right end of the texture and if so then start at the beginning again.
					if (tuCount == incrementCount)
					{
						tuCoordinate = 0.0f;
						tuCount = 0;
					}
				}

				// Increment the tv texture coordinate by the increment value and increment the index by one.
				tvCoordinate -= incrementValue;
				tvCount++;

				// Check if at the top of the texture and if so then start at the bottom again.
				if (tvCount == incrementCount)
				{
					tvCoordinate = 1.0f;
					tvCount = 0;
				}
			}
		}

		private XYZType GetNormalForPosition(int x, int z)
		{
			var index = GetIndexFromPosition(x, z);
			var right = GetRelativeIndex(RelativePosition.Right, index);
			var bottom = GetRelativeIndex(RelativePosition.BottomMiddle, index);

			var vertex1 = new Vector3(HeightMap[index].x, HeightMap[index].y, HeightMap[index].z);
			var vertex2 = new Vector3(HeightMap[right].x, HeightMap[right].y, HeightMap[right].z);
			var vertex3 = new Vector3(HeightMap[bottom].x, HeightMap[bottom].y, HeightMap[bottom].z);

			var vector1 = vertex1 - vertex3;
			var vector2 = vertex3 - vertex2;

			var crossProduct = Vector3.Cross(vector1, vector2);

			var normal = new XYZType()
			{
				x = crossProduct.X,
				y = crossProduct.Y,
				z = crossProduct.Z
			};

			return normal;
		}

		private bool PositionNotOnLeftBorder(int x)
		{
			return x >= 1;
		}

		private bool PositionNotOnSampleRightBorder(int x)
		{
			var sampleRight = _size.Width - 1;

			return x < sampleRight;
		}

		private bool PositionNotOnTopBorder(int z)
		{
			return z >= 1;
		}

		private bool PositionNotOnSampleBottomBorder(int z)
		{
			var sampleBottom = _size.Height - 1;

			return z < sampleBottom;
		}

		private Vector3 AddNormalToVectorSum(Vector3 sum, XYZType normal)
		{
			var newSum = new Vector3()
			{
				X = sum[0] + normal.x,
				Y = sum[1] + normal.y,
				Z = sum[2] + normal.z
			};

			return newSum;
		}

		private XYZType[] GetNewNormals()
		{
			var sampleHeight = _size.Height - 1;
			var sampleWidth = _size.Width - 1;

			var normals = new XYZType[sampleHeight * sampleWidth];

			for (var z = 0; z < sampleHeight; ++z)
			{
				for (var x = 0; x < sampleWidth; ++x)
				{
					var normal = GetNormalForPosition(x, z);

					var sampleIndex = (z * sampleHeight) + x;

					normals[sampleIndex] = normal;
				}
			}

			return normals;
		}

		private Dictionary<Coordinate2D<int>, XYZType> _cachedNormals;

		private XYZType GetCachedNormalForPosition(int x, int z)
		{
			var cachedNormalExists = _cachedNormals.Any(e => e.Key.X == x && e.Key.Y == z);

			if (!cachedNormalExists)
			{
				var normal = GetNormalForPosition(x, z);

				_cachedNormals.Add(new Coordinate2D<int>(x, z), normal);
			}

			return _cachedNormals.First(e => e.Key.X == x && e.Key.Y == z).Value;
		}

		private bool UpdateNormal(int x, int z)
		{
			var sampleHeight = _size.Height - 1;

			var sum = Vector3.Zero;

			var count = 9;

			if (PositionNotOnLeftBorder(x) && PositionNotOnTopBorder(z))
			{
				var topLeft = GetCachedNormalForPosition(x - 1, z - 1);
				sum = AddNormalToVectorSum(sum, topLeft);
				count++;
			}

			if (PositionNotOnSampleRightBorder(x) && PositionNotOnTopBorder(z))
			{
				var topRight = GetCachedNormalForPosition(x + 1, z - 1);
				sum = AddNormalToVectorSum(sum, topRight);
				count++;
			}

			if (PositionNotOnLeftBorder(x) && PositionNotOnSampleBottomBorder(z))
			{
				var bottomLeft = GetCachedNormalForPosition(x - 1, z + 1);
				sum = AddNormalToVectorSum(sum, bottomLeft);
				count++;
			}

			if (PositionNotOnSampleRightBorder(x) && PositionNotOnSampleBottomBorder(z))
			{
				var bottomRight = GetCachedNormalForPosition(x + 1, z + 1);
				sum = AddNormalToVectorSum(sum, bottomRight);
				count++;
			}

			sum.X = sum.X / count;
			sum.Y = sum.Y / count;
			sum.Z = sum.Z / count;

			var index = GetIndexFromPosition(x, z);

			var length = sum.Length();

			ChangeNormal(index, sum.X / length, sum.Y / length, sum.Z / length);

			return true;
		}

		private bool UpdateNormals(XYZType[] normals)
		{
			var actualHeight = _size.Height;
			var actualWidth = _size.Width;

			var sampleHeight = actualHeight - 1;
			var sampleWidth = actualWidth - 1;

			for (var z = 0; z < actualHeight; ++z)
			{
				for (var x = 0; x < actualWidth; ++x)
				{
					var sum = Vector3.Zero;

					var count = 9;

					if (PositionNotOnLeftBorder(x) && PositionNotOnTopBorder(z))
					{
						var topLeft = ((z - 1) * sampleHeight) + x - 1;
						sum = AddNormalToVectorSum(sum, normals[topLeft]);
						count++;
					}

					if (PositionNotOnSampleRightBorder(x) && PositionNotOnTopBorder(z))
					{
						var topRight = ((z - 1) * sampleHeight) + x;
						sum = AddNormalToVectorSum(sum, normals[topRight]);
						count++;
					}

					if (PositionNotOnLeftBorder(x) && PositionNotOnSampleBottomBorder(z))
					{
						var bottomLeft = (z * sampleHeight) + x - 1;
						sum = AddNormalToVectorSum(sum, normals[bottomLeft]);
						count++;
					}

					if (PositionNotOnSampleRightBorder(x) && PositionNotOnSampleBottomBorder(z))
					{
						var bottomRight = (z * sampleHeight) + x;
						sum = AddNormalToVectorSum(sum, normals[bottomRight]);
						count++;
					}

					sum.X = sum.X / count;
					sum.Y = sum.Y / count;
					sum.Z = sum.Z / count;

					var index = GetIndexFromPosition(x, z);

					var length = sum.Length();

					ChangeNormal(index, sum.X / length, sum.Y / length, sum.Z / length);
				}
			}

			normals = null;

			return true;
		}

		private bool CalculateNormals()
		{
			var newNormals = GetNewNormals();

			var result = UpdateNormals(newNormals);

			return true;
			
			///////////////

			//int index;
			//float length;
			//Vector3 vertex1, vertex2, vertex3, vector1, vector2, sum;
			//var normals = new XYZType[(_size.Height - 1) * (_size.Width - 1)];
			//
			//for (int j = 0; j < (_size.Height - 1); j++)
			//{
			//	for (int i = 0; i < (_size.Width - 1); i++)
			//	{
			//		int index1 = (j * _size.Height) + i;		// center
			//		int index2 = (j * _size.Height) + (i + 1);	// right
			//		int index3 = ((j + 1) * _size.Height) + i;	// bottom
			//
			//		vertex1.X = HeightMap[index1].x;
			//		vertex1.Y = HeightMap[index1].y;
			//		vertex1.Z = HeightMap[index1].z;
			//
			//		vertex2.X = HeightMap[index2].x;
			//		vertex2.Y = HeightMap[index2].y;
			//		vertex2.Z = HeightMap[index2].z;
			//
			//		vertex3.X = HeightMap[index3].x;
			//		vertex3.Y = HeightMap[index3].y;
			//		vertex3.Z = HeightMap[index3].z;
			//
			//		vector1 = vertex1 - vertex3;
			//		vector2 = vertex3 - vertex2;
			//
			//		index = (j * (_size.Height - 1)) + i;
			//
			//		Vector3 vecTestCrossProduct = Vector3.Cross(vector1, vector2);
			//		normals[index].x = vecTestCrossProduct.X;
			//		normals[index].y = vecTestCrossProduct.Y;
			//		normals[index].z = vecTestCrossProduct.Z;
			//	}
			//}

			// j = z (up/down)
			// i = x (left/right)
			//for (int j = 0; j < _size.Height; j++)
			//{
			//	for (int i = 0; i < _size.Width; i++)
			//	{
			//		sum = Vector3.Zero;
			//
			//		int count = 9;
			//
			//		if (((i - 1) >= 0) && ((j - 1) >= 0))	// not hard left & not hard top
			//		{
			//			index = ((j - 1) * (_size.Height - 1)) + (i - 1);
			//
			//			sum[0] += normals[index].x;
			//			sum[1] += normals[index].y;
			//			sum[2] += normals[index].z;
			//			count++;
			//		}
			//		if ((i < (_size.Width - 1)) && ((j - 1) >= 0))	// not hard sample right & not hard top
			//		{
			//			index = ((j - 1) * (_size.Height - 1)) + i;
			//
			//			sum[0] += normals[index].x;
			//			sum[1] += normals[index].y;
			//			sum[2] += normals[index].z;
			//			count++;
			//		}
			//		if (((i - 1) >= 0) && (j < (_size.Height - 1)))	// not hard left & not hard sample bottom
			//		{
			//			index = (j * (_size.Height - 1)) + (i - 1);
			//
			//			sum[0] += normals[index].x;
			//			sum[1] += normals[index].y;
			//			sum[2] += normals[index].z;
			//			count++;
			//		}
			//		if ((i < (_size.Width - 1)) && (j < (_size.Height - 1)))	// not hard sample right & not hard sample bottom
			//		{
			//			index = (j * (_size.Height - 1)) + i;
			//
			//			sum.X += normals[index].x;
			//			sum.Y += normals[index].y;
			//			sum.Z += normals[index].z;
			//			count++;
			//		}
			//
			//		sum.X = sum.X / count;
			//		sum.Y = sum.Y / count;
			//		sum.Z = sum.Z / count;
			//
			//		length = (float)Math.Sqrt((sum.X * sum.X) + (sum.Y * sum.Y) + (sum.Z * sum.Z));
			//
			//		index = (j * _size.Height) + i;
			//
			//		var editHeightMap = HeightMap[index];
			//		editHeightMap.nx = (sum.X / length);
			//		editHeightMap.ny = (sum.Y / length);
			//		editHeightMap.nz = (sum.Z / length);
			//		HeightMap[index] = editHeightMap;
			//	}
			//}
			//
			//normals = null;
			//
			//return true;
		}

		private bool LoadHeightMap(string heightMapFileName)
		{
			Bitmap bitmap;

			try
			{
				bitmap = new Bitmap(SystemConfiguration.DataFilePath + heightMapFileName);
			}
			catch
			{
				return false;
			}

			_size.Width = bitmap.Width;
			_size.Height = bitmap.Height;

			HeightMap = new List<XYZTextureNormalType>(_size.Width * _size.Height);

			for (var j = 0; j < _size.Height; ++j)
			{
				for (var i = 0; i < _size.Width; ++i)
				{
					HeightMap.Add(new XYZTextureNormalType()
					{
						x = i,
						y = bitmap.GetPixel(i, j).R,
						z = j
					});
				}
			}

			bitmap.Dispose();
			bitmap = null;

			return true;
		}

		private bool InitialiseBuffers(Device device)
		{
			try
			{
				/// Calculate the number of vertices in the terrain mesh.
				VertexCount = (_size.Width - 1) * (_size.Height - 1) * 6;

				Vertices = new PositionTextureNormalVertex[VertexCount];

				// Set the index count to the same as the vertex count.
				IndexCount = VertexCount;

				// Create the index array.
				int[] indices = new int[IndexCount];

				// Initialize the index to the vertex array.
				int index = 0;

				// Load the vertex and index arrays with the terrain data.
				for (int j = 0; j < (_size.Height - 1); j++)
				{
					for (int i = 0; i < (_size.Width - 1); i++)
					{
						int indexBottomLeft1 = (_size.Height * j) + i;          // Bottom left.
						int indexBottomRight2 = (_size.Height * j) + (i + 1);      // Bottom right.
						int indexUpperLeft3 = (_size.Height * (j + 1)) + i;      // Upper left.
						int indexUpperRight4 = (_size.Height * (j + 1)) + (i + 1);  // Upper right.

						#region First Triangle

						// Upper left.
						float tv = HeightMap[indexUpperLeft3].tv;
						// Modify the texture coordinates to cover the top edge.
						if (tv == 1.0f)
							tv = 0.0f;

						Vertices[index] = new PositionTextureNormalVertex()
						{
							position = new Vector3(HeightMap[indexUpperLeft3].x, HeightMap[indexUpperLeft3].y, HeightMap[indexUpperLeft3].z),
							texture = new Vector2(HeightMap[indexUpperLeft3].tu, tv),
							normal = new Vector3(HeightMap[indexUpperLeft3].nx, HeightMap[indexUpperLeft3].ny, HeightMap[indexUpperLeft3].nz)
						};
						indices[index] = index++;

						// Upper right.
						float tu = HeightMap[indexUpperRight4].tu;
						tv = HeightMap[indexUpperRight4].tv;

						// Modify the texture coordinates to cover the top and right edge.
						if (tu == 0.0f)
							tu = 1.0f;
						if (tv == 1.0f)
							tv = 0.0f;

						Vertices[index] = new PositionTextureNormalVertex()
						{
							position = new Vector3(HeightMap[indexUpperRight4].x, HeightMap[indexUpperRight4].y, HeightMap[indexUpperRight4].z),
							texture = new Vector2(tu, tv),
							normal = new Vector3(HeightMap[indexUpperRight4].nx, HeightMap[indexUpperRight4].ny, HeightMap[indexUpperRight4].nz)
						};
						indices[index] = index++;

						// Bottom left.
						Vertices[index] = new PositionTextureNormalVertex()
						{
							position = new Vector3(HeightMap[indexBottomLeft1].x, HeightMap[indexBottomLeft1].y, HeightMap[indexBottomLeft1].z),
							texture = new Vector2(HeightMap[indexBottomLeft1].tu, HeightMap[indexBottomLeft1].tv),
							normal = new Vector3(HeightMap[indexBottomLeft1].nx, HeightMap[indexBottomLeft1].ny, HeightMap[indexBottomLeft1].nz)
						};
						indices[index] = index++;
						#endregion

						#region Second Triangle
						// Bottom left.
						Vertices[index] = new PositionTextureNormalVertex()
						{
							position = new Vector3(HeightMap[indexBottomLeft1].x, HeightMap[indexBottomLeft1].y, HeightMap[indexBottomLeft1].z),
							texture = new Vector2(HeightMap[indexBottomLeft1].tu, HeightMap[indexBottomLeft1].tv),
							normal = new Vector3(HeightMap[indexBottomLeft1].nx, HeightMap[indexBottomLeft1].ny, HeightMap[indexBottomLeft1].nz)
						};
						indices[index] = index++;

						// Upper right.
						tu = HeightMap[indexUpperRight4].tu;
						tv = HeightMap[indexUpperRight4].tv;

						// Modify the texture coordinates to cover the top and right edge.
						if (tu == 0.0f)
							tu = 1.0f;
						if (tv == 1.0f)
							tv = 0.0f;

						Vertices[index] = new PositionTextureNormalVertex()
						{
							position = new Vector3(HeightMap[indexUpperRight4].x, HeightMap[indexUpperRight4].y, HeightMap[indexUpperRight4].z),
							texture = new Vector2(tu, tv),
							normal = new Vector3(HeightMap[indexUpperRight4].nx, HeightMap[indexUpperRight4].ny, HeightMap[indexUpperRight4].nz)
						};
						indices[index] = index++;

						// Bottom right.
						tu = HeightMap[indexBottomRight2].tu;

						// Modify the texture coordinates to cover the right edge.
						if (tu == 0.0f)
							tu = 1.0f;

						Vertices[index] = new PositionTextureNormalVertex()
						{
							position = new Vector3(HeightMap[indexBottomRight2].x, HeightMap[indexBottomRight2].y, HeightMap[indexBottomRight2].z),
							texture = new Vector2(tu, HeightMap[indexBottomRight2].tv),
							normal = new Vector3(HeightMap[indexBottomRight2].nx, HeightMap[indexBottomRight2].ny, HeightMap[indexBottomRight2].nz)
						};
						indices[index] = index++;
						#endregion
					}
				}

				// Create the vertex buffer.
				_vertexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, Vertices);

				// Create the index buffer.
				_indexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.IndexBuffer, indices);

				// Release the arrays now that the buffers have been created and loaded.
				indices = null;

				return true;
			}
			catch
			{
				return false;
			}
		}

		private void RenderBuffers(DeviceContext deviceContext)
		{
			deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, SharpDX.Utilities.SizeOf<PositionTextureNormalVertex>(), 0));
			deviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
			deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
		}
	}
}
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.IO;
using Visualiser.Containers;
using Visualiser.Containers.Vertices;

namespace Visualiser.Graphics.Components
{
	public class Font : IDisposable
	{
		public int SpaceSize { get; set; }
		public float Height { get; set; }
		public FontType[] Fonts { get; set; }
		public Texture Texture { get; private set; }

		public bool Initialise(Device device, string fontFileName, string textureFileName, float height, int spaceSize)
		{
			Height = height;
			SpaceSize = spaceSize;

			var result = LoadFontData(fontFileName);

			result &= LoadTexture(device, textureFileName);

			return result;
		}

		public void Dispose()
		{
			Texture?.Dispose();
			Texture = null;

			if (Fonts != null)
			{
				Fonts = null;
			}
		}

		public void BuildVertexArray(out List<PositionTextureVertex> vertices, string sentence, Coordinate2D<float> position)
		{
			vertices = new List<PositionTextureVertex>();

			foreach (var character in sentence)
			{
				var letter = character - 32;

				if (letter == 0)
				{
					position.X += 3;
				}
				else
				{
					BuildVertexArray(vertices, letter, position);

					position.X += Fonts[letter].size + 1;
				}
			}
		}

		private void BuildVertexArray(List<PositionTextureVertex> vertices, int letter, Coordinate2D<float> position)
		{
			vertices.Add(new PositionTextureVertex()
			{
				position = new Vector3(position.X, position.Y, 0),
				texture = new Vector2(Fonts[letter].left, 0)
			});

			vertices.Add(new PositionTextureVertex()
			{
				position = new Vector3(position.X + Fonts[letter].size, position.Y - Height, 0),
				texture = new Vector2(Fonts[letter].right, 1)
			});

			vertices.Add(new PositionTextureVertex()
			{
				position = new Vector3(position.X, position.Y - Height, 0),
				texture = new Vector2(Fonts[letter].left, 1)
			});

			vertices.Add(new PositionTextureVertex()
			{
				position = new Vector3(position.X, position.Y, 0),
				texture = new Vector2(Fonts[letter].left, 0)
			});

			vertices.Add(new PositionTextureVertex()
			{
				position = new Vector3(position.X + Fonts[letter].size, position.Y, 0),
				texture = new Vector2(Fonts[letter].right, 0)
			});

			vertices.Add(new PositionTextureVertex()
			{
				position = new Vector3(position.X + Fonts[letter].size, position.Y - Height, 0),
				texture = new Vector2(Fonts[letter].right, 1)
			});
		}

		private bool LoadFontData(string fontFileName)
		{
			Fonts = new FontType[95];

			try
			{
				fontFileName = SystemConfiguration.FontFilePath + fontFileName;

				var fontDataLines = File.ReadAllLines(fontFileName);

				var index = 0;

				foreach (var line in fontDataLines)
				{
					var modelArray = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

					Fonts[index++] = new FontType()
					{
						left = float.Parse(modelArray[modelArray.Length - 3]),
						right = float.Parse(modelArray[modelArray.Length - 2]),
						size = int.Parse(modelArray[modelArray.Length - 1]),
					};
				}

				fontDataLines = null;

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private bool LoadTexture(Device device, string textureFileName)
		{
			textureFileName = SystemConfiguration.FontFilePath + textureFileName;

			Texture = new Texture();

			var result = Texture.Initialise(device, textureFileName);

			return result;
		}
	}
}

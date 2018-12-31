using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using Visualiser.Containers;
using Visualiser.Containers.Vertices;
using Visualiser.Utilities;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Visualiser.Graphics.Objects
{
	public class Foliage : IDisposable
	{
		public int FoliageCount { get; set; }
		public int VertexCount { get; set; }
		public int InstanceCount { get; set; }
		public FoliageType[] FoliageArray { get; set; }
		public InstanceType[] Instances { get; set; }
		public Buffer VertexBuffer { get; set; }
		public Buffer InstanceBuffer { get; set; }
		public Texture Texture { get; set; }
		public float WindRotation { get; set; }
		public int WindDirection { get; set; }

		public bool Initialise(Device device, string textureFileName, int foliageCount)
		{
			FoliageCount = foliageCount;

			var result = GeneratePositions();
			result &= InitialiseBuffers(device);
			result &= LoadTexture(device, textureFileName);

			WindRotation = 0.9f;
			WindDirection = 1;

			return result;
		}

		public void Dispose()
		{
			ReleaseTexture();
			ShutdownBuffers();

			if (FoliageArray != null)
			{
				FoliageArray = null;
			}
		}

		public void Render(DeviceContext deviceContext)
		{
			RenderBuffers(deviceContext);
		}

		public bool Frame(Coordinate3D<float> cameraPosition, DeviceContext deviceContext)
		{
			if (WindDirection == 1)
			{
				WindRotation += 0.1f;

				if (WindRotation > 10.0f)
				{
					WindDirection = 2;
				}
			}
			else
			{
				WindRotation -= 0.1f;
				if (WindRotation < -10.0f)
				{
					WindDirection = 1;
				}
			}

			double angle;
			float rotation;
			Vector3 modelPosition;
			Matrix rotateMatrix;
			Matrix rotateMatrix2;
			Matrix translationMatrix;
			Matrix finalMatrix;
			float windRotation;

			for (int i = 0; i < FoliageCount; ++i)
			{
				modelPosition = new Vector3()
				{
					X = FoliageArray[i].x,
					Y = -0.1f,
					Z = FoliageArray[i].z
				};

				angle = System.Math.Atan2(modelPosition.X - cameraPosition.X, modelPosition.Z - cameraPosition.Z) * (180 / Constants.Pi);

				rotation = (float)angle * Constants.RadiansPerDegree;
				rotateMatrix = Matrix.RotationY(rotation);
				windRotation = WindRotation * Constants.RadiansPerDegree;
				rotateMatrix2 = Matrix.RotationX(windRotation);
				translationMatrix = Matrix.Translation(modelPosition.X, modelPosition.Y, modelPosition.Z);

				finalMatrix = Matrix.Multiply(rotateMatrix, rotateMatrix2);
				Instances[i].matrix = Matrix.Multiply(finalMatrix, translationMatrix);
			}

			deviceContext.MapSubresource(InstanceBuffer, MapMode.WriteDiscard, MapFlags.None, out DataStream mappedResource);
			mappedResource.WriteRange(Instances);

			deviceContext.UnmapSubresource(InstanceBuffer, 0);

			return true;
		}

		private bool LoadTexture(Device device, string textureFileName)
		{
			Texture = new Texture();

			var result = Texture.Initialise(device, SystemConfiguration.DataFilePath + textureFileName);

			return result;
		}

		private bool InitialiseBuffers(Device device)
		{
			VertexCount = 6;

			var vertices = new PositionTextureVertex[VertexCount];

			vertices[0].position = new Vector3(0.0f, 0.0f, 0.0f);
			vertices[0].texture = new Vector2(0.0f, 1.0f);

			vertices[1].position = new Vector3(0.0f, 1.0f, 0.0f);
			vertices[1].texture = new Vector2(0.0f, 0.0f);

			vertices[2].position = new Vector3(1.0f, 0.0f, 0.0f);
			vertices[2].texture = new Vector2(1.0f, 1.0f);

			vertices[3].position = new Vector3(1.0f, 0.0f, 0.0f);
			vertices[3].texture = new Vector2(1.0f, 1.0f);

			vertices[4].position = new Vector3(0.0f, 1.0f, 0.0f);
			vertices[4].texture = new Vector2(0.0f, 0.0f);

			vertices[5].position = new Vector3(1.0f, 1.0f, 0.0f);
			vertices[5].texture = new Vector2(1.0f, 0.0f);

			VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices);

			vertices = null;

			InstanceCount = FoliageCount;

			Instances = new InstanceType[InstanceCount];

			var matrix = Matrix.Identity;

			for (int i = 0; i < InstanceCount; ++i)
			{
				Instances[i].matrix = matrix;
				Instances[i].colour = new Vector3(FoliageArray[i].r, FoliageArray[i].g, FoliageArray[i].b);
			}

			var instanceBufferDescription = new BufferDescription()
			{
				Usage = ResourceUsage.Dynamic,
				SizeInBytes = SharpDX.Utilities.SizeOf<InstanceType>() * InstanceCount,
				BindFlags = BindFlags.VertexBuffer,
				CpuAccessFlags = CpuAccessFlags.Write,
				OptionFlags = ResourceOptionFlags.None,
				StructureByteStride = 0
			};

			InstanceBuffer = Buffer.Create(device, Instances, instanceBufferDescription);

			return true;
		}

		private bool GeneratePositions()
		{
			var maximum = 32767;

			FoliageArray = new FoliageType[FoliageCount];

			var random = new Random((int)DateTime.Now.Ticks);

			for (int i = 0; i < FoliageCount; ++i)
			{
				var testX = (float)random.Next(maximum) / maximum * 9.0f - 4.5f;
				var testZ = (float)random.Next(maximum) / maximum * 9.0f - 4.5f;
				FoliageArray[i].x = testX;
				FoliageArray[i].z = testZ;

				var red = (float)random.Next(maximum) / maximum;
				var green = (float)random.Next(maximum) / maximum;
				FoliageArray[i].r = red + 1.0f;
				FoliageArray[i].g = green + 0.5f;
			}

			return true;
		}

		private void ShutdownBuffers()
		{
			InstanceBuffer?.Dispose();
			InstanceBuffer = null;

			VertexBuffer?.Dispose();
			VertexBuffer = null;

			Instances = null;
		}

		private void ReleaseTexture()
		{
			Texture?.Dispose();
			Texture = null;
		}

		private void RenderBuffers(DeviceContext deviceContext)
		{
			deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(VertexBuffer, SharpDX.Utilities.SizeOf<PositionTextureVertex>(), 0), new VertexBufferBinding(InstanceBuffer, SharpDX.Utilities.SizeOf<InstanceType>(), 0));

			deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
		}
	}
}

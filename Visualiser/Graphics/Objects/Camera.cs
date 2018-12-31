using SharpDX;
using System;
using Visualiser.Containers;
using Visualiser.Utilities;
using Math = System.Math;

namespace Visualiser.Graphics
{
	public class Camera
	{
		public Camera()
		{
			Position = new Coordinate3D<float>();
			Rotation = new Coordinate3D<float>();
		}

		public Coordinate3D<float> Position { get; set; }
		public Coordinate3D<float> Rotation { get; set; }
		public Matrix ViewMatrix { get; private set; }
		public Matrix BaseViewMatrix { get; private set; }

		public void SetPosition(Coordinate3D<float> position)
		{
			Position.CopyFrom(position);
		}

		public void Render()
		{
			var position = new Vector3(Position.X, Position.Y, Position.Z);
			var lookAt = new Vector3(0, 0, 1);

			var pitch = Rotation.X * Constants.RadiansPerDegree;
			var yaw = Rotation.Y * Constants.RadiansPerDegree;
			var roll = Rotation.Z * Constants.RadiansPerDegree;

			var rotationMatrix = Matrix.RotationYawPitchRoll(yaw, pitch, roll);

			lookAt = Vector3.TransformCoordinate(lookAt, rotationMatrix);

			var up = Vector3.TransformCoordinate(Vector3.UnitY, rotationMatrix);

			lookAt = position + lookAt;

			ViewMatrix = Matrix.LookAtLH(position, lookAt, up);
		}

		public void RenderBaseViewMatrix()
		{
			var up = Vector3.Up;

			var radians = Rotation.Y * Constants.RadiansPerDegree;

			var lookAt = new Vector3()
			{
				X = (float)Math.Sin(radians) + Position.X,
				Y = Position.Y,
				Z = (float)Math.Cos(radians) + Position.Z
			};

			var position = new Vector3(Position.X, Position.Y, Position.Z);

			BaseViewMatrix = Matrix.LookAtLH(position, lookAt, up);
		}
	}
}
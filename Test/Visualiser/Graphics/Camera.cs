using SharpDX;
using Visualiser.Containers;

namespace Visualiser.Graphics
{
	public class Camera
	{
		private const float RadiansPerDegree = 0.0174532925f;

		public Camera()
		{
			Position = new Coordinate3D<float>();
			Rotation = new Coordinate3D<float>();
		}

		public Coordinate3D<float> Position { get; set; }
		public Coordinate3D<float> Rotation { get; set; }
		public Matrix ViewMatrix { get; private set; }

		public void SetPosition(Coordinate3D<float> position)
		{
			Position.CopyFrom(position);
		}

		public void Render()
		{
			var position = new Vector3(Position.X, Position.Y, Position.Z);
			var lookAt = new Vector3(0, 0, 1);

			var pitch = Rotation.X * RadiansPerDegree;
			var yaw = Rotation.Y * RadiansPerDegree;
			var roll = Rotation.Z * RadiansPerDegree;

			var rotationMatrix = Matrix.RotationYawPitchRoll(yaw, pitch, roll);

			lookAt = Vector3.TransformCoordinate(lookAt, rotationMatrix);

			var up = Vector3.TransformCoordinate(Vector3.UnitY, rotationMatrix);

			lookAt = position + lookAt;

			ViewMatrix = Matrix.LookAtLH(position, lookAt, up);
		}
	}
}
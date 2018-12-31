using SharpDX;
using System;

namespace Test.Graphics.Objects
{
	public class Camera
	{
		private const float RadiansPerDegree = 0.0174532925f;

		private Vector3 _position;
		private Vector3 _rotation;

		public Camera()
		{
			_position = new Vector3();
			_rotation = new Vector3();
		}

		public Matrix ViewMatrix { get; private set; }
		public Matrix BaseViewMatrix { get; private set; }

		public void SetPosition(float x, float y, float z)
		{
			_position.X = x;
			_position.Y = y;
			_position.Z = z;
		}

		public void SetRotation(float x, float y, float z)
		{
			_rotation.X = x;
			_rotation.Y = y;
			_rotation.Z = z;
		}
		public Vector3 GetPosition()
		{
			return new Vector3(_position.X, _position.Y, _position.Z);
		}

		public void Render()
		{
			Vector3 lookAt = new Vector3(0, 0, 1.0f);

			float pitch = _rotation.X * RadiansPerDegree;
			float yaw = _rotation.Y * RadiansPerDegree;
			float roll = _rotation.Z * RadiansPerDegree;

			Matrix rotationMatrix = Matrix.RotationYawPitchRoll(yaw, pitch, roll);

			lookAt = Vector3.TransformCoordinate(lookAt, rotationMatrix);
			Vector3 up = Vector3.TransformCoordinate(Vector3.UnitY, rotationMatrix);

			lookAt = _position + lookAt;

			ViewMatrix = Matrix.LookAtLH(_position, lookAt, up);
		}

		internal void RenderBaseViewMatrix()
		{
			Vector3 up = Vector3.Up;

			float radians = _rotation.Y * RadiansPerDegree;

			Vector3 lookAt = new Vector3
			{
				X = (float)Math.Sin(radians) + _position.X,
				Y = _position.Y,
				Z = (float)Math.Cos(radians) + _position.Z
			};

			BaseViewMatrix = Matrix.LookAtLH(GetPosition(), lookAt, up);
		}
	}
}
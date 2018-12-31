using SharpDX;
using System.Diagnostics;
using Visualiser.Containers;
using Visualiser.Utilities;

using Math = System.Math;

namespace Visualiser.Graphics.Objects
{
	public class Player
	{
		private Velocity3D _velocity;
		private LookDirection2D _lookDirection;
		private float _verticalVelocity;

		public Player()
		{
			_velocity = new Velocity3D();
			_lookDirection = new LookDirection2D();
			_verticalVelocity = 0.0f;
		}

		public Coordinate3D<float> Position { get; set; }
		public Coordinate3D<float> Rotation { get; set; }

		public void Update(float frameTime, float heightDifference, MovementInput input)
		{
			_velocity.Update(frameTime, input);
			_lookDirection.Update(frameTime, input);

			if (input.Jump && heightDifference < 0.1f)
			{
				_verticalVelocity = 1.0f;
			}

			if (_verticalVelocity > -10.0f)
			{
				_verticalVelocity -= 0.05f;
			}

			Rotation.X += _lookDirection.Down;
			Rotation.X = Rotation.X.Clamp(-90, 90);

			Rotation.Y += _lookDirection.Right;
			Rotation.Y = Rotation.Y.RotationLock();

			var radians = Rotation.Y * Constants.RadiansPerDegree;
			var sideways = (Rotation.Y + 90) * Constants.RadiansPerDegree;

			var forwardX = (float)Math.Sin(radians) * _velocity.NetForward;
			var forwardZ = (float)Math.Cos(radians) * _velocity.NetForward;

			var sidewaysX = (float)Math.Sin(sideways) * _velocity.NetRightward;
			var sidewaysZ = (float)Math.Cos(sideways) * _velocity.NetRightward;

			var netMovement = new Vector2(forwardX + sidewaysX, forwardZ + sidewaysZ);

			var maxVelocity = Constants.MaxVelocity;

			if (input.Sprint)
			{
				maxVelocity *= 3;
			}

			if (netMovement.Length() > maxVelocity)
			{
				netMovement = Vector2.Normalize(netMovement);
				netMovement = Vector2.Multiply(netMovement, maxVelocity);
			}

			Position.X += netMovement.X;
			Position.Y += _verticalVelocity;
			Position.Z += netMovement.Y;

			//Position.X += (float)Math.Sin(radians) * _velocity.NetForward;
			//Position.Y += _velocity.NetUpward;
			//Position.Z += (float)Math.Cos(radians) * _velocity.NetForward;
		}
	}
}

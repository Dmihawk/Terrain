using Visualiser.Utilities;

namespace Visualiser.Containers
{
	public class LookDirection2D
	{
		public const float Acceleration = 0.01f;
		public const float Deceleration = -0.005f;

		//public float Left { get; set; }
		//public float Right { get; set; }
		//public float Up { get; set; }
		//public float Down { get; set; }

		public int Right { get; private set; }
		public int Down { get; private set; }

		//public float NetRight
		//{
		//	get { return Right - Left; }
		//}
		//
		//public float NetDown
		//{
		//	get { return Down - Up; }
		//}

		public void Update(float frameTime, MovementInput input)
		{
			Right = input.LookRight / 2;
			Down = input.LookDown / 2;

			//Left += input.LookRight * frameTime;
			//Right += (input.Rightward ? Acceleration : Deceleration) * frameTime;
			//Up += input.LookDown * frameTime;
			//Down += (input.LookDown ? Acceleration : Deceleration) * frameTime;
			//
			//var maxVelocity = frameTime * 0.15f;
			//
			//Left = Left.Clamp(0, maxVelocity);
			//Right = Right.Clamp(0, maxVelocity);
			//Up = Up.Clamp(0, maxVelocity);
			//Down = Down.Clamp(0, maxVelocity);
		}
	}
}
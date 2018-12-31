using Visualiser.Utilities;

namespace Visualiser.Containers
{
	public class Velocity3D
	{
		private const float Acceleration = 0.001f;
		//private const float Deceleration = -0.0007f;
		private const float Deceleration = -10.00f;

		public float Forward { get; set; }
		public float Backward { get; set; }
		public float Leftward { get; set; }
		public float Rightward { get; set; }
		public float Upward { get; set; }
		public float Downward { get; set; }

		public float NetForward
		{
			get { return Forward - Backward; }
		}

		public float NetUpward
		{
			get { return Upward - Downward; }
		}

		public float NetRightward
		{
			get { return Rightward - Leftward; }
		}

		public void Update(float frameTime, MovementInput input)
		{
			Forward += (input.Forward ? Acceleration : Deceleration) * frameTime;
			Backward += (input.Backward ? Acceleration : Deceleration) * frameTime;
			Leftward += (input.Leftward ? Acceleration : Deceleration) * frameTime;
			Rightward += (input.Rightward ? Acceleration : Deceleration) * frameTime;
			Upward += (input.Upward ? Acceleration : Deceleration) * frameTime;
			Downward += (input.Downward ? Acceleration : Deceleration) * frameTime;

			var maxVelocity = frameTime * 0.03f;

			Forward = Forward.Clamp(0.0f, maxVelocity);
			Backward = Backward.Clamp(0.0f, maxVelocity);
			Leftward = Leftward.Clamp(0.0f, maxVelocity);
			Rightward = Rightward.Clamp(0.0f, maxVelocity);
			Upward = Upward.Clamp(0.0f, maxVelocity);
			Downward = Downward.Clamp(0.0f, maxVelocity);
		}
	}
}

using Logging;
using SharpDX;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Visualiser.Containers;

namespace Visualiser.Utilities
{
	public class Input : IDisposable
	{
		private KeyboardState _keyboardState;
		public MouseState _mouseState;

		public DirectInput DirectInput { get; set; }
		public Keyboard Keyboard { get; set; }
		public Mouse Mouse { get; set; }
		public Dimension ScreenSize;
		public Coordinate2D<int> MousePosition;

		public bool Initialise(Dimension screenSize, IntPtr windowHandle)
		{
			ScreenSize = new Dimension(screenSize.Width, screenSize.Height);
			MousePosition = new Coordinate2D<int>(0, 0);
			DirectInput = new DirectInput();

			Keyboard = new Keyboard(DirectInput);
			Keyboard.Properties.BufferSize = 256;
			Keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);

			var result = true;

			try
			{
				Keyboard.Acquire();
			}
			catch (SharpDXException ex)
			{
				Log.WriteToFile(ErrorLevel.Error, "Input.Initialise (Keyboard)", ex, true);

				result = false;
			}

			Mouse = new Mouse(DirectInput);
			Mouse.Properties.AxisMode = DeviceAxisMode.Relative;
			Mouse.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);

			try
			{
				Mouse.Acquire();
			}
			catch (SharpDXException ex)
			{
				Log.WriteToFile(ErrorLevel.Error, "Input.Initialise (Mouse)", ex, true);

				result = false;
			}

			return result;
		}

		public void Dispose()
		{
			Mouse?.Unacquire();
			Mouse?.Dispose();
			Mouse = null;

			Keyboard.Unacquire();
			Keyboard.Dispose();
			Keyboard = null;

			DirectInput.Dispose();
		}

		public bool Frame()
		{
			var result = true;

			result &= ReadKeyboard();
			result &= ReadMouse();

			if (result)
			{
				ProcessInput();
			}

			return result;
		}

		public bool IsEscapePressed
		{
			get
			{
				return _keyboardState != null && _keyboardState.PressedKeys.Contains(Key.Escape);
			}
		}

		private bool ReadKeyboard()
		{
			var result = true;

			var resultCode = ResultCode.Ok;
			_keyboardState = new KeyboardState();

			try
			{
				Keyboard.GetCurrentState(ref _keyboardState);
			}
			catch (SharpDXException ex)
			{
				Log.WriteToFile(ErrorLevel.Warning, "Input.ReadKeyboard (SharpDXException)", ex, true);

				resultCode = ex.Descriptor;
			}
			catch (Exception ex)
			{
				Log.WriteToFile(ErrorLevel.Error, "Input.ReadKeyboard", ex, true);

				result = false;
			}

			if (result && (resultCode == ResultCode.InputLost || resultCode == ResultCode.NotAcquired))
			{
				try
				{
					Keyboard.Acquire();
				}
				catch (Exception ex)
				{
					Log.WriteToFile(ErrorLevel.Error, "Input.ReadKeyboard (Reacquire)", ex, true);

					result = false;
				}
			}

			result &= resultCode == ResultCode.Ok;

			return result;
		}

		private bool ReadMouse()
		{
			var result = true;

			var resultCode = ResultCode.Ok;
			_mouseState = new MouseState();

			try
			{
				Mouse.GetCurrentState(ref _mouseState);
			}
			catch (SharpDXException ex)
			{
				Log.WriteToFile(ErrorLevel.Warning, "Input.ReadMouse (SharpDXException)", ex, true);

				resultCode = ex.Descriptor;
			}
			catch (Exception ex)
			{
				Log.WriteToFile(ErrorLevel.Error, "Input.ReadMouse", ex, true);

				result = false;
			}

			if (result && (resultCode == ResultCode.InputLost || resultCode == ResultCode.NotAcquired))
			{
				try
				{
					Mouse.Acquire();
				}
				catch (Exception ex)
				{
					Log.WriteToFile(ErrorLevel.Error, "Input.ReadMouse (Reacquire)", ex, true);

					result = false;
				}
			}

			result &= resultCode == ResultCode.Ok;

			return result;
		}

		private void ProcessInput()
		{
			if (_mouseState != null)
			{
				MousePosition.X += _mouseState.X;
				MousePosition.Y += _mouseState.Y;
			}

			MousePosition.X = MousePosition.X.Clamp(0, ScreenSize.Width);
			MousePosition.Y = MousePosition.Y.Clamp(0, ScreenSize.Height);
		}
	}
}
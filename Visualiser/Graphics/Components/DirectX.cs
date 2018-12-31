using System;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Visualiser.Containers;

using DirectXDevice = SharpDX.Direct3D11.Device;
using FillMode = SharpDX.Direct3D11.FillMode;
using Resource = SharpDX.Direct3D11.Resource;

namespace Visualiser.Graphics
{
	public class DirectX : IDisposable
	{
		private const Format RGB8_UNorm = Format.R8G8B8A8_UNorm;

		private bool _verticalSyncEnabled;
		private SwapChain _swapChain;
		private RenderTargetView _renderTargetView;
		private Texture2D _depthStencilBuffer;
		private DepthStencilView _depthStencilView;
		private RasterizerState _rasterState;

		public DirectX()
		{

		}

		public int VideoCardMemory { get; private set; }
		public string VideoCardDescription { get; private set; }
		public DirectXDevice Device { get; private set; }
		public DeviceContext DeviceContext { get; private set; }
		public DepthStencilState DepthStencilState { get; set; }
		public Matrix ProjectionMatrix { get; private set; }
		public Matrix WorldMatrix { get; private set; }
		public Matrix OrthoMatrix { get; private set; }
		public DepthStencilState DepthDisabledStencilState { get; private set; }
		public BlendState AlphaEnableBlendingState { get; private set; }
		public BlendState AlphaDisableBlendingState { get; private set; }
		public BlendState AlphaAdditiveBlendState2 { get; private set; }
		public ViewportF ViewPort { get; set; }

		public bool Initialise(Dimension size, IntPtr windowHandle)
		{
			try
			{
				_verticalSyncEnabled = SystemConfiguration.VerticalSyncEnabled;

				var refreshRate = GetRefreshRateAndSetVideoCardProperties(size);

				SetDeviceAndSwapChain(size, windowHandle, refreshRate);
				CreateRenderTargetView();
				CreateDepthBufferAndStencils(size);
				SetRasterState(size);
				SetMatrices(size);
				CreateDepthDisabledStencil();
				CreateBlendStates();

				return true;
			}
			catch (Exception ex)
			{
				//Log.WriteToFile(ErrorLevel.Error, "DirectX.Initialise", ex, true);

				return false;
			}
		}

		public void Dispose()
		{
			_swapChain?.SetFullscreenState(false, null);

			AlphaAdditiveBlendState2?.Dispose();
			AlphaAdditiveBlendState2 = null;

			AlphaEnableBlendingState?.Dispose();
			AlphaEnableBlendingState = null;

			AlphaDisableBlendingState?.Dispose();
			AlphaDisableBlendingState = null;

			DepthDisabledStencilState?.Dispose();
			DepthDisabledStencilState = null;

			_rasterState?.Dispose();
			_rasterState = null;

			_depthStencilView?.Dispose();
			_depthStencilView = null;

			DepthStencilState?.Dispose();
			DepthStencilState = null;

			_depthStencilBuffer?.Dispose();
			_depthStencilBuffer = null;

			_renderTargetView?.Dispose();
			_renderTargetView = null;

			DeviceContext?.Dispose();
			DeviceContext = null;

			Device?.Dispose();
			Device = null;

			_swapChain?.Dispose();
			_swapChain = null;
		}

		public void SetAlphaBlending(bool on)
		{
			var blendFactor = new Color4(0, 0, 0, 0);

			var blendState = on ? AlphaEnableBlendingState : AlphaDisableBlendingState;

			DeviceContext.OutputMerger.SetBlendState(blendState, blendFactor, -1);
		}

		public void SetZBuffer(bool on)
		{
			var depthStencilState = on ? DepthStencilState : DepthDisabledStencilState;

			DeviceContext.OutputMerger.SetDepthStencilState(depthStencilState, 1);
		}

		public void EnableSecondBlendState()
		{
			var blendFactor = new Color4(0, 0, 0, 0);

			DeviceContext.OutputMerger.SetBlendState(AlphaAdditiveBlendState2, blendFactor, -1);
		}

		public void BeginScene(Color4 colour)
		{
			DeviceContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1, 0);
			DeviceContext.ClearRenderTargetView(_renderTargetView, colour);
		}

		public void EndScene()
		{
			var syncInterval = _verticalSyncEnabled ? 1 : 0;

			_swapChain.Present(syncInterval, PresentFlags.None);
		}

		private Rational GetRefreshRateAndSetVideoCardProperties(Dimension size)
		{
			var refreshRate = new Rational(0, 1);

			using (var factory = new Factory1())
			using (var adapter = factory.GetAdapter1(0))
			using (var monitor = adapter.GetOutput(0))
			{
				var modes = monitor.GetDisplayModeList(RGB8_UNorm, DisplayModeEnumerationFlags.Interlaced);

				if (_verticalSyncEnabled)
				{
					foreach (var mode in modes)
					{
						if (size.SameSizeAs(mode))
						{
							refreshRate = new Rational(mode.RefreshRate.Numerator, mode.RefreshRate.Denominator);
							break;
						}
					}
				}

				var adapterDescription = adapter.Description;
				VideoCardMemory = adapterDescription.DedicatedVideoMemory / 1024 / 1024; // In MB
				VideoCardDescription = adapterDescription.Description.Trim('\0');
			}

			return refreshRate;
		}

		private void SetDeviceAndSwapChain(Dimension size, IntPtr windowHandle, Rational refreshRate)
		{
			var swapChainDescription = new SwapChainDescription()
			{
				BufferCount = 1,
				ModeDescription = new ModeDescription(size.Width, size.Height, refreshRate, RGB8_UNorm),
				Usage = Usage.RenderTargetOutput,
				OutputHandle = windowHandle,
				SampleDescription = new SampleDescription(1, 0),
				IsWindowed = !SystemConfiguration.FullScreen,
				Flags = SwapChainFlags.None,
				SwapEffect = SwapEffect.Discard
			};

			DirectXDevice.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, swapChainDescription, out DirectXDevice device, out _swapChain);
			Device = device;
			DeviceContext = device.ImmediateContext;
		}

		private void CreateRenderTargetView()
		{
			using (var backBuffer = Resource.FromSwapChain<Texture2D>(_swapChain, 0))
			{
				_renderTargetView = new RenderTargetView(Device, backBuffer);
			}
		}

		private void CreateDepthBufferAndStencils(Dimension size)
		{
			CreateDepthBuffer(size);
			CreateDepthStencilState();
			CreateDepthStencilView();
		}

		private void CreateDepthBuffer(Dimension size)
		{
			var depthBufferDescription = new Texture2DDescription()
			{
				Width = size.Width,
				Height = size.Height,
				MipLevels = 1,
				ArraySize = 1,
				Format = Format.D24_UNorm_S8_UInt,
				SampleDescription = new SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.DepthStencil,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None
			};

			_depthStencilBuffer = new Texture2D(Device, depthBufferDescription);
		}

		private void CreateDepthStencilState()
		{
			var depthStencilDescription = new DepthStencilStateDescription()
			{
				IsDepthEnabled = true,
				DepthWriteMask = DepthWriteMask.All,
				DepthComparison = Comparison.Less,
				IsStencilEnabled = true,
				StencilReadMask = 0xFF,
				StencilWriteMask = 0xFF,
				FrontFace = new DepthStencilOperationDescription()
				{
					FailOperation = StencilOperation.Keep,
					DepthFailOperation = StencilOperation.Increment,
					PassOperation = StencilOperation.Keep,
					Comparison = Comparison.Always
				},
				BackFace = new DepthStencilOperationDescription()
				{
					FailOperation = StencilOperation.Keep,
					DepthFailOperation = StencilOperation.Decrement,
					PassOperation = StencilOperation.Keep,
					Comparison = Comparison.Always
				}
			};

			DepthStencilState = new DepthStencilState(Device, depthStencilDescription);
			DeviceContext.OutputMerger.SetDepthStencilState(DepthStencilState, 1);
		}

		private void CreateDepthStencilView()
		{
			var depthStencilViewDescription = new DepthStencilViewDescription()
			{
				Format = Format.D24_UNorm_S8_UInt,
				Dimension = DepthStencilViewDimension.Texture2D,
				Texture2D = new DepthStencilViewDescription.Texture2DResource()
				{
					MipSlice = 0
				}
			};

			_depthStencilView = new DepthStencilView(Device, _depthStencilBuffer, depthStencilViewDescription);
			DeviceContext.OutputMerger.SetTargets(_depthStencilView, _renderTargetView);
		}

		private void SetRasterState(Dimension size)
		{
			var rasterDescription = new RasterizerStateDescription()
			{
				IsAntialiasedLineEnabled = false,
				CullMode = CullMode.Back,
				DepthBias = 0,
				DepthBiasClamp = 0.0f,
				IsDepthClipEnabled = true,
				FillMode = FillMode.Solid,
				IsFrontCounterClockwise = false,
				IsMultisampleEnabled = false,
				IsScissorEnabled = false,
				SlopeScaledDepthBias = 0.0f
			};

			_rasterState = new RasterizerState(Device, rasterDescription);

			DeviceContext.Rasterizer.State = _rasterState;
			ViewPort = new ViewportF(0.0f, 0.0f, size.Width, size.Height, 0.0f, 1.0f);
			DeviceContext.Rasterizer.SetViewport(ViewPort);
		}

		private void SetMatrices(Dimension size)
		{
			var fov = (float)Math.PI / 4.0f;
			var aspect = size.AspectRatio;
			var zNear = SystemConfiguration.ScreenNear;
			var zFar = SystemConfiguration.ScreenDepth;

			ProjectionMatrix = Matrix.PerspectiveFovLH(fov, aspect, zNear, zFar);

			WorldMatrix = Matrix.Identity;

			OrthoMatrix = Matrix.OrthoLH(size.Width, size.Height, zNear, zFar);
		}

		private void CreateDepthDisabledStencil()
		{
			var depthDisabledStencilDescription = new DepthStencilStateDescription()
			{
				IsDepthEnabled = false,
				DepthWriteMask = DepthWriteMask.All,
				DepthComparison = Comparison.Less,
				IsStencilEnabled = true,
				StencilReadMask = 0xFF,
				StencilWriteMask = 0xFF,
				FrontFace = new DepthStencilOperationDescription()
				{
					FailOperation = StencilOperation.Keep,
					DepthFailOperation = StencilOperation.Increment,
					PassOperation = StencilOperation.Keep,
					Comparison = Comparison.Always
				},
				BackFace = new DepthStencilOperationDescription()
				{
					FailOperation = StencilOperation.Keep,
					DepthFailOperation = StencilOperation.Decrement,
					PassOperation = StencilOperation.Keep,
					Comparison = Comparison.Always
				}
			};

			DepthDisabledStencilState = new DepthStencilState(Device, depthDisabledStencilDescription);
		}

		private void CreateBlendStates()
		{
			var blendStateDescription = new BlendStateDescription()
			{
				AlphaToCoverageEnable = false
			};

			blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
			blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.One;
			blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.One;
			blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
			blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
			blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
			blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
			blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

			AlphaEnableBlendingState = new BlendState(Device, blendStateDescription);

			blendStateDescription.AlphaToCoverageEnable = false;
			blendStateDescription.RenderTarget[0].IsBlendEnabled = false;

			AlphaDisableBlendingState = new BlendState(Device, blendStateDescription);

			var secondaryBlendStateDescription = new BlendStateDescription()
			{
				AlphaToCoverageEnable = true
			};

			secondaryBlendStateDescription.RenderTarget[0].IsBlendEnabled = true;
			secondaryBlendStateDescription.RenderTarget[0].SourceBlend = BlendOption.One;
			secondaryBlendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
			secondaryBlendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
			secondaryBlendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
			secondaryBlendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
			secondaryBlendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
			secondaryBlendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

			AlphaAdditiveBlendState2 = new BlendState(Device, secondaryBlendStateDescription);
		}
	}
}
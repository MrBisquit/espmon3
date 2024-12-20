﻿using System;

namespace EspMon
{
	class MessageWindow : IDisposable
	{
		public const int WM_CUSTOM_ACTIVATE = 0x801C;
		WeakReference<MainWindow> _appWindow;
		public static MessageWindow Window { get; private set; }
		delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		[System.Runtime.InteropServices.StructLayout(
			System.Runtime.InteropServices.LayoutKind.Sequential,
		   CharSet = System.Runtime.InteropServices.CharSet.Unicode
		)]
		struct WNDCLASS
		{
			public uint style;
			public IntPtr lpfnWndProc;
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public IntPtr hbrBackground;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
			public string lpszMenuName;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
			public string lpszClassName;
		}

		[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
		static extern System.UInt16 RegisterClassW(
			[System.Runtime.InteropServices.In] ref WNDCLASS lpWndClass
		);

		[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr CreateWindowExW(
		   UInt32 dwExStyle,
		   [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
	   string lpClassName,
		   [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
	   string lpWindowName,
		   UInt32 dwStyle,
		   Int32 x,
		   Int32 y,
		   Int32 nWidth,
		   Int32 nHeight,
		   IntPtr hWndParent,
		   IntPtr hMenu,
		   IntPtr hInstance,
		   IntPtr lpParam
		);

		[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
		static extern System.IntPtr DefWindowProcW(
			IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam
		);

		[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
		static extern bool DestroyWindow(
			IntPtr hWnd
		);

		private const int ERROR_CLASS_ALREADY_EXISTS = 1410;

		private bool m_disposed;
		private IntPtr m_hwnd;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!m_disposed)
			{
				// Dispose unmanaged resources
				if (m_hwnd != IntPtr.Zero)
				{
					DestroyWindow(m_hwnd);
					m_hwnd = IntPtr.Zero;
				}
				m_disposed = true;
			}
		}
		~MessageWindow()
		{
			Dispose(false);
		}
		public MessageWindow(MainWindow appWindow)
		{
			if (Window != null)
			{
				throw new InvalidOperationException("A message window already exists");
			}
			_appWindow = new WeakReference<MainWindow>(appWindow);
			m_wnd_proc_delegate = ForwardWndProc;

			// Create WNDCLASS
			WNDCLASS wind_class = new WNDCLASS();
			wind_class.lpszClassName = "CEspMon";
			wind_class.lpfnWndProc = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(m_wnd_proc_delegate);

			UInt16 class_atom = RegisterClassW(ref wind_class);

			int last_error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

			if (class_atom == 0 && last_error != ERROR_CLASS_ALREADY_EXISTS)
			{
				throw new System.Exception("Could not register window class");
			}
			Window = this;
			// Create window
			m_hwnd = CreateWindowExW(
				0,
				"CEspMon",
				"Esp Mon Activator",
				0,
				0,
				0,
				0,
				0,
				IntPtr.Zero,
				IntPtr.Zero,
				IntPtr.Zero,
				IntPtr.Zero
			);
			
		}
		private MainWindow _AppWindow
		{
			get
			{
				if (m_disposed)
				{
					throw new ObjectDisposedException(nameof(MessageWindow));
				}
				MainWindow result;
				if (_appWindow.TryGetTarget(out result))
				{
					return result;
				}
				throw new InvalidOperationException("The application is closing");
			}
		}
		private static IntPtr ForwardWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			if(msg==WM_CUSTOM_ACTIVATE && Window!=null && Window._AppWindow!=null)
			{
				Window._AppWindow.ActivateApp();
			}
			return DefWindowProcW(hWnd, msg, wParam, lParam);
		}

		private WndProc m_wnd_proc_delegate;
	}
}

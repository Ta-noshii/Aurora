using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Aurora.Utils;
using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;
using User32 = Aurora.Utils.User32;

namespace Aurora.Modules.Inputs;

/// <summary>
/// Class for subscribing to various HID input events
/// </summary>
public sealed class InputEvents : IInputEvents
{
    /// <summary>
    /// Event for a Key pressed Down on a keyboard
    /// </summary>
    public event EventHandler<KeyboardKeyEvent>? KeyDown;

    /// <summary>
    /// Event for a Key released on a keyboard
    /// </summary>
    public event EventHandler<KeyboardKeyEvent>? KeyUp;

    /// <summary>
    /// Event that fires when a mouse button is pressed down.
    /// </summary>
    public event EventHandler<MouseKeyEvent>? MouseButtonDown;

    /// <summary>
    /// Event that fires when a mouse button is released.
    /// </summary>
    public event EventHandler<MouseKeyEvent>? MouseButtonUp;

    /// <summary>
    /// Event that fires when the mouse scroll wheel is scrolled.
    /// </summary>
    public event EventHandler<MouseScrollEvent>? Scroll;

    private readonly List<Keys> _pressedKeySequence = new();

    private readonly List<MouseButtons> _pressedMouseButtons = new();

    private bool _disposed;

    public IReadOnlyList<Keys> PressedKeys => new ReadOnlyCollection<Keys>(_pressedKeySequence.ToArray());

    public IReadOnlyList<MouseButtons> PressedButtons => new ReadOnlyCollection<MouseButtons>(_pressedMouseButtons.ToArray());

    private static readonly Keys[] ShiftKeys = {Keys.ShiftKey, Keys.RShiftKey, Keys.LShiftKey};
    public bool Shift => ShiftKeys.Any(PressedKeys.Contains);

    private static readonly Keys[] AltKeys = {Keys.Menu, Keys.RMenu, Keys.LMenu};
    public bool Alt => AltKeys.Any(PressedKeys.Contains);

    private static readonly Keys[] CtrlKeys = {Keys.ControlKey, Keys.RControlKey, Keys.LControlKey};
    public bool Control => CtrlKeys.Any(PressedKeys.Contains);

    private static readonly Keys[] WinKeys = { Keys.LWin, Keys.RWin };
    public bool Windows => WinKeys.Any(PressedKeys.Contains);

    private delegate nint WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable to keep reference for garbage collector
    private readonly WndProc? _fnWndProcHook;
    private readonly nint _originalWndProc;
    private readonly IntPtr _hWnd;

    public InputEvents()
    {
        _hWnd = User32.CreateWindowEx(0, "STATIC", "", 0x80000000, 0, 0,
            0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        _originalWndProc = User32.GetWindowLongPtr(_hWnd, -4);

        // register the keyboard device and you can register device which you need like mouse
        RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, _hWnd);
        RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, _hWnd);

        _fnWndProcHook = Hook;
        nint newLong = Marshal.GetFunctionPointerForDelegate(_fnWndProcHook);
        User32.SetWindowLongPtr(_hWnd, -4, newLong);
    }
    
    private nint Hook(IntPtr hwnd, uint msg, IntPtr wparam, IntPtr lparam)
    {
        const int wmInput = 0x00FF;

        // You can read inputs by processing the WM_INPUT message.
        if (msg != wmInput) return User32.CallWindowProc(_originalWndProc, _hWnd, msg, wparam, lparam);
        // Create an RawInputData from the handle stored in lParam.
        var data = RawInputData.FromHandle(lparam);

        // The data will be an instance of either RawInputMouseData, RawInputKeyboardData, or RawInputHidData.
        // They contain the raw input data in their properties.
        var intercepted = data switch
        {
            RawInputMouseData mouse => DeviceOnMouseInput(mouse.Mouse),
            RawInputKeyboardData keyboard => DeviceOnKeyboardInput(keyboard.Keyboard),
            _ => false,
        };

        return intercepted ? IntPtr.Zero : User32.CallWindowProc(_originalWndProc, _hWnd, msg, wparam, lparam);
    }

    /// <param name="keyboardData"></param>
    /// <returns>if input should be interrupted or not</returns>
    private bool DeviceOnKeyboardInput(RawKeyboard keyboardData)
    {
        try
        {
            var flags = keyboardData.Flags;
            // e0 and e1 are escape sequences used for certain special keys, such as PRINT and PAUSE/BREAK.
            // see http://www.win.tue.nl/~aeb/linux/kbd/scancodes-1.html
            var isE0 = flags.HasFlag(RawKeyboardFlags.KeyE0);
            var isE1 = flags.HasFlag(RawKeyboardFlags.KeyE1);
            var key = KeyUtils.CorrectRawInputData(keyboardData.VirutalKey, keyboardData.ScanCode, isE0, isE1);
            if ((int)key == 255)
            {
                // discard "fake keys" which are part of an escaped sequence
                return false;
            }

            var keyboardKeyEvent = new KeyboardKeyEvent(key, flags.HasFlag(RawKeyboardFlags.KeyE0));
            if ((flags & RawKeyboardFlags.Up) != 0)
            {
                _pressedKeySequence.RemoveAll(k => k == key);
                KeyUp?.Invoke(this, keyboardKeyEvent);
            }
            else
            {
                if (!_pressedKeySequence.Contains(key))
                    _pressedKeySequence.Add(key);
                KeyDown?.Invoke(this, keyboardKeyEvent);
            }

            return keyboardKeyEvent.Intercepted;
        }
        catch (Exception exc)
        {
            Global.logger.Error(exc, "Exception while handling keyboard input");
            return false;
        }
    }

    /// <summary>
    /// Handles a SharpDX MouseInput event and fires the relevant InputEvents event (Scroll, MouseButtonDown or MouseButtonUp).
    /// <returns>if input should be interrupted or not</returns>
    /// </summary>
    private bool DeviceOnMouseInput(RawMouse mouseData)
    {
        // Scrolling
        if (mouseData.ButtonData != 0)
        {
            if (mouseData.Buttons == RawMouseButtonFlags.MouseWheel)
            {
                var mouseScrollEvent = new MouseScrollEvent(mouseData.ButtonData);
                Scroll?.Invoke(this, mouseScrollEvent);

                return mouseScrollEvent.Intercepted;
            }

            return false;
        }

        var (button, isDown) = mouseData.Buttons switch
        {
            RawMouseButtonFlags.LeftButtonDown => (MouseButtons.Left, true),
            RawMouseButtonFlags.LeftButtonUp => (MouseButtons.Left, false),
            RawMouseButtonFlags.MiddleButtonDown => (MouseButtons.Middle, true),
            RawMouseButtonFlags.MiddleButtonUp => (MouseButtons.Middle, false),
            RawMouseButtonFlags.RightButtonDown => (MouseButtons.Right, true),
            RawMouseButtonFlags.RightButtonUp => (MouseButtons.Right, false),
            _ => (MouseButtons.Left, false)
        };

        var mouseKeyEvent = new MouseKeyEvent(button);
        if (isDown)
        {
            if (!_pressedMouseButtons.Contains(button))
                _pressedMouseButtons.Add(button);
            MouseButtonDown?.Invoke(this, mouseKeyEvent);
        }
        else
        {
            _pressedMouseButtons.Remove(button);
            MouseButtonUp?.Invoke(this, mouseKeyEvent);
        }

        return mouseKeyEvent.Intercepted;
    }

    public TimeSpan GetTimeSinceLastInput() {
        var inf = new User32.tagLASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<User32.tagLASTINPUTINFO>() };
        return !User32.GetLastInputInfo(ref inf) ?
            new TimeSpan(0) :
            new TimeSpan(0, 0, 0, 0, Environment.TickCount - inf.dwTime);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
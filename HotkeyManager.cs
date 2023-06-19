using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Hotkeys
{
    public sealed class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly static int WM_HOTKEY = 0x0312;

        private class Window : NativeWindow, IDisposable
        {
            public Window()
            {
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if (m.Msg == WM_HOTKEY)
                {
                    Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                    ModKeys modifier = (ModKeys)((int)m.LParam & 0xFFFF);
                    ThreadPool.QueueUserWorkItem(HandleHotkey, new KeyPressedEventArgs(modifier, key));
                }
            }

            public void HandleHotkey(object state)
            {
                KeyPressed?.Invoke(this, (KeyPressedEventArgs)state);
            }

            public event EventHandler<KeyPressedEventArgs> KeyPressed;

            #region IDisposable Members
            public void Dispose()
            {
                DestroyHandle();
            }
            #endregion
        }

        readonly private Window _window = new Window();
        private int _currentId = 0;

        public HotkeyManager()
        {
            _window.KeyPressed += delegate (object HotkeySender, KeyPressedEventArgs args)
            {
                KeyPressed?.Invoke(this, args);
            };
        }

        private  readonly List<int> _registeredHotkeyIds = new List<int>();

        public void RegisterHotKey(ModKeys modifier, Keys key)
        {
            uint fsModifiers = (uint)modifier | (uint)ModKeys.NoRepeat;
            if (RegisterHotKey(_window.Handle, _currentId, fsModifiers, (uint)key))
            {
                _registeredHotkeyIds.Add(_currentId);
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            foreach (int hotkeyId in _registeredHotkeyIds)
            {
                UnregisterHotKey(_window.Handle, hotkeyId);
            }
        }
        #endregion

        public static event EventHandler<KeyPressedEventArgs> KeyPressed;
    }

    public class KeyPressedEventArgs : EventArgs
    {
        private readonly ModKeys _modkey;
        private readonly Keys _key;

        internal KeyPressedEventArgs(ModKeys modkey, Keys key)
        {
            _modkey = modkey;
            _key = key;
        }

        public ModKeys Modkey
        {
            get { return _modkey; }
        }

        public Keys Key
        {
            get { return _key; }
        }
    }

    [Flags]
    public enum ModKeys : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8,
        NoRepeat = 16384
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MyMineBlock
{
    public sealed class HotKeyManager : IDisposable
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
                    windowKeyPressed?.Invoke(this, new KeyPressedEventArgs(modifier, key));
                }
            }

            public event EventHandler<KeyPressedEventArgs> windowKeyPressed;

            #region IDisposable Members
            public void Dispose()
            {
                DestroyHandle();
            }
            #endregion
        }

        readonly private Window window = new Window();
        private int currentId = 0;

        public HotKeyManager()
        {
            window.windowKeyPressed += delegate (object HotKeySender, KeyPressedEventArgs args)
            {
                keyPressed?.Invoke(this, args);
            };
        }

        private readonly List<HotKey> registeredHotkeys = new List<HotKey>();

        public void RegisterHotKey(ModKeys modifier, Keys key)
        {
            uint fsModifiers = (uint)modifier | (uint)ModKeys.NoRepeat;
            if (RegisterHotKey(window.Handle, currentId, fsModifiers, (uint)key))
            {
                HotKey newHotKey = new HotKey
                {
                    Modifier = modifier,
                    Key = key,
                    Id = currentId
                };
                registeredHotkeys.Add(newHotKey);
                currentId++;
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            for (int i = currentId; i > -1; i--)
            {
                UnregisterHotKey(window.Handle, i);
            }
            currentId = 0;
            registeredHotkeys.Clear();
        }
        #endregion

        public static event EventHandler<KeyPressedEventArgs> keyPressed;
    }

    public class KeyPressedEventArgs : EventArgs
    {
        private readonly ModKeys _modkey;
        private readonly Keys _key;

        public ModKeys Modkey
        {
            get { return _modkey; }
        }

        public Keys Key
        {
            get { return _key; }
        }

        internal KeyPressedEventArgs(ModKeys modkey, Keys key)
        {
            _modkey = modkey;
            _key = key;
        }
    }

    public class HotKey
    {
        public ModKeys Modifier { get; set; }
        public Keys Key { get; set; }
        public int Id { get; set; }
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

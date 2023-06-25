using System.Runtime.InteropServices;

namespace MyMineBlock
{
    public sealed partial class HotKeyManager : IDisposable
    {
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

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

            public event EventHandler<KeyPressedEventArgs>? windowKeyPressed;

            #region IDisposable Members
            public void Dispose()
            {
                DestroyHandle();
            }
            #endregion
        }

        readonly private Window listenerWindow = new();
        private int currentId = 0;

        public HotKeyManager()
        {
            listenerWindow.windowKeyPressed += (HotKeySender, args) =>
            {
                if (args is not null)
                {
                    keyPressed?.Invoke(this, args);
                }
            };
        }

        private readonly List<HotKey> registeredHotKeys = new();

        public void RegisterHotKey(ModKeys modifier, Keys key)
        {
            if (RegisterHotKey(listenerWindow.Handle, currentId, (uint)modifier, (uint)key))
            {
                HotKey newHotKey = new()
                {
                    Modifier = modifier,
                    Key = key,
                    Id = currentId
                };
                registeredHotKeys.Add(newHotKey);
                currentId++;
            }
        }
        public void RegisterHotKey(ModKeys[] modifiers, Keys key)
        {
            uint fsModifiers = 0;
            foreach (ModKeys modifier in modifiers)
            {
                fsModifiers |= (uint)modifier;
            }
            if (RegisterHotKey(listenerWindow.Handle, currentId, fsModifiers, (uint)key))
            {
                HotKey newHotKey = new()
                {
                    Modifiers = modifiers,
                    Key = key,
                    Id = currentId
                };
                registeredHotKeys.Add(newHotKey);
                currentId++;
            }
        }

        public void RegisterHotKeyNoRepeat(ModKeys modifier, Keys key)
        {
            uint fsModifiers = (uint)modifier | (uint)ModKeys.NoRepeat;
            if (RegisterHotKey(listenerWindow.Handle, currentId, fsModifiers, (uint)key))
            {
                HotKey newHotKey = new()
                {
                    Modifier = modifier,
                    Key = key,
                    Id = currentId
                };
                registeredHotKeys.Add(newHotKey);
                currentId++;
            }
        }
        public void RegisterHotKeyNoRepeat(ModKeys[] modifiers, Keys key)
        {
            uint fsModifiers = 0;
            foreach (ModKeys modifier in modifiers)
            {
                if (modifier != ModKeys.NoRepeat)
                {
                    fsModifiers |= (uint)modifier;
                }
            }
            fsModifiers |= (uint)ModKeys.NoRepeat;
            if (RegisterHotKey(listenerWindow.Handle, currentId, fsModifiers, (uint)key))
            {
                HotKey newHotKey = new()
                {
                    Modifiers = modifiers,
                    Key = key,
                    Id = currentId
                };
                registeredHotKeys.Add(newHotKey);
                currentId++;
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            for (int i = currentId; i > -1; i--)
            {
                UnregisterHotKey(listenerWindow.Handle, i);
            }
            currentId = 0;
            registeredHotKeys.Clear();
            keyPressed = null;
        }
        #endregion

        public static event EventHandler<KeyPressedEventArgs>? keyPressed;
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

    public struct HotKey
    {
        public ModKeys Modifier { get; set; }
        public ModKeys[] Modifiers { get; set; }
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

using Microsoft.Win32;
using System.Collections.Generic;
using System.Drawing;

namespace IrcClient
{
    public static class Configuration
    {
        private const string RegistryKey = @"Software\nProg\WpfIrcClient";

        #region Registry Functions
        private static Dictionary<string, object> _cache = new Dictionary<string, object>();

        private static int ReadInteger(string name, int defaultValue = 0)
        {
            object cachedValue;
            if (_cache.TryGetValue(name, out cachedValue))
                return (int)cachedValue;

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey))
                {
                    var val = key.GetValue(name);
                    if (val != null &&
                        val is int)
                        return (int)val;
                }
            }
            catch { }

            return defaultValue;
        }

        private static void WriteInteger(string name, int value)
        {
            _cache[name] = value;
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryKey))
                key.SetValue(name, value, RegistryValueKind.DWord);
        }

        private static string ReadString(string name, string defaultValue = null)
        {
            object cachedValue;
            if (_cache.TryGetValue(name, out cachedValue) &&
                cachedValue is string)
                return (string)cachedValue;

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey))
                {
                    var val = key.GetValue(name);
                    if (val != null &&
                        val is string)
                        return (string)val;
                }
            }
            catch { }

            return defaultValue;
        }

        private static void WriteString(string name, string value)
        {
            _cache[name] = value;
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryKey))
                key.SetValue(name, value, RegistryValueKind.String);
        }
        #endregion

        public static bool PlaySoundOnMessages
        {
            get
            {
                return ReadInteger("PlaySoundOnMessages", 1) == 1;
            }
            set
            {
                WriteInteger("PlaySoundOnMessages", value ? 1 : 0);
            }
        }

        public static bool FlashWindow
        {
            get
            {
                return ReadInteger("FlashWindow", 1) == 1;
            }
            set
            {
                WriteInteger("FlashWindow", value ? 1 : 0);
            }
        }

        public static bool LineSplitMessages
        {
            get
            {
                return ReadInteger("LineSplitMessages", 1) == 1;
            }
            set
            {
                WriteInteger("LineSplitMessages", value ? 1 : 0);
            }
        }

        public static Point WindowLocation
        {
            get
            {
                return new Point(ReadInteger("X", 10), ReadInteger("Y", 10));
            }
            set
            {
                WriteInteger("X", value.X);
                WriteInteger("Y", value.Y);
            }
        }

        public static Size WindowSize
        {
            get
            {
                return new Size(ReadInteger("Width", 800), ReadInteger("Height", 600));
            }
            set
            {
                WriteInteger("Width", value.Width);
                WriteInteger("Height", value.Height);
            }
        }

        public static string MyNick
        {
            get
            {
                return ReadString("MyNick");
            }
            set
            {
                WriteString("MyNick", value);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IrcClient.Network
{
    public static class NetworkLogger
    {
        private static readonly string _filename;
        private static long _count = 0;

        static NetworkLogger()
        {
            var date = DateTime.Now;

            var path = Environment.OSVersion.Platform == PlatformID.Unix ?
                "netlogs" :
                "netlogs";

            _filename = Path.Combine(path, string.Format("{0:0000}-{1:00}-{2:00}", date.Year, date.Month, date.Day));
            Directory.CreateDirectory(_filename);

            _filename = Path.Combine(_filename, string.Format("{0:00}.{1:00}.{2:00}.txt", date.Hour, date.Minute, date.Second));
            _filename = Path.Combine(Environment.CurrentDirectory, _filename);
        }

        private static void LogText(string format, params object[] args)
        {
            var sb = new StringBuilder(256);

            if (Interlocked.Increment(ref _count) > 1)
                sb.Append("\r\n\r\n");

            var date = DateTime.Now;
            sb.AppendFormat("[{0:00}:{1:00}:{2:00}] ", date.Hour, date.Minute, date.Second);
            sb.Append(string.Format(format, args));

            File.AppendAllText(_filename, sb.ToString());
        }

        public static void LogConnected(string host, int port)
        {
            LogText("Connected to {0}:{1}", host, port);
        }

        public static void LogDisconnected()
        {
            LogText("Connection closed");
        }

        public static void LogIncoming(string data)
        {
            LogText("Incoming {0} bytes\r\n{1}", data.Length, data);
        }

        public static void LogOutgoing(string data)
        {
            LogText("Outgoing {0} bytes\r\n{1}", data.Length, data);
        }
    }
}

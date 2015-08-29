using IrcClient.IRC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IrcClient.Network
{
    public partial class IrcNetworkClient : NetworkClientBase
    {
        private readonly StringBuilder _buffer = new StringBuilder(1024);
        private readonly Dictionary<string, MethodInfo> _packetMethods = new Dictionary<string, MethodInfo>();

        private readonly string _myNick;
        public string MyNick { get { return _myNick; } }

        private readonly ChannelManager _channelManager = new ChannelManager();
        public ChannelManager Channels { get { return _channelManager; } }

        public IrcNetworkClient(string myNick)
        {
            if (string.IsNullOrWhiteSpace(_myNick = myNick))
                throw new ArgumentException("Client nick name cannot be empty.");

            foreach (var mi in GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                var attribute = mi.GetCustomAttribute<PacketAttribute>();
                if (attribute != null)
                    _packetMethods.Add(attribute.Header, mi);
            }
        }

        protected override void OnDispose()
        {
        }

        protected override void OnConnected()
        {
            var address = Socket.RemoteEndPoint.ToString().Split(':');
            NetworkLogger.LogConnected(address[0], int.Parse(address[1]));

            Send(string.Format("NICK {0}\r\nUSER {0} 0 * :{0}\r\n", MyNick));
        }

        protected override void OnDisconnected()
        {
            NetworkLogger.LogDisconnected();
        }

        protected override void OnData(byte[] buffer, int length)
        {
            var data = _buffer.ToString() + Encoding.GetEncoding(1252).GetString(buffer, 0, length);
            NetworkLogger.LogIncoming(data);
            _buffer.Clear();

            int pos;
            while ((pos = data.IndexOf('\n')) != -1)
            {
                var packet = data.Substring(0, pos);
                data = data.Substring(pos + 1);

                if (packet.Length > 0)
                {
                    if (packet.EndsWith("\r"))
                    {
                        packet = packet.Substring(0, packet.Length - 1);
                        if (packet.Length == 0)
                            continue;
                    }

                    // log packet
                    System.IO.File.AppendAllText("datalog.txt", string.Format("[{0}]\r\n{1}\r\n\r\n", DateTime.Now.ToString("HH:mm:ss"), packet));

                    string host = string.Empty;

                    if (packet[0] == ':')
                    {
                        if ((pos = packet.IndexOf(' ')) == -1)
                        {
                            System.IO.File.AppendAllText("datalog.txt", "[WARNING] DISCARDED (No content in packet)\r\n\r\n");
                            continue;
                        }

                        host = packet.Substring(1, pos - 1);

                        packet = packet.Substring(pos + 1);
                        if (packet.Length == 0)
                            continue;
                    }

                    string content = string.Empty;
                    if ((pos = packet.IndexOf(' ')) != -1)
                    {
                        content = packet.Substring(pos + 1);
                        packet = packet.Substring(0, pos);
                    }

                    if (!PreprocessPacket(host, packet, content))
                    {
                        // do something ...
                        System.IO.File.AppendAllText("datalog.txt", "[WARNING] Preprocess packet failed\r\n\r\n");
                    }
                }
            }
        }

        public int Send(string data)
        {
            NetworkLogger.LogOutgoing(data);
            var binary = Encoding.GetEncoding(1252).GetBytes(data);
            return Send(binary, binary.Length);
        }

        public int SendFormat(string format, params object[] args)
        {
            return Send(string.Format(format, args));
        }

        private bool PreprocessPacket(string host, string header, string content)
        {
            var x_nick = string.Empty;
            var x_account = string.Empty;
            var x_host = string.Empty;
            int pos;

            var host_x = host;
            if ((pos = host_x.IndexOf('@')) != -1) // strip away hostname
            {
                x_host = host_x.Substring(pos + 1);
                host_x = host_x.Substring(0, pos);
            }

            if ((pos = host_x.IndexOf('!')) != -1) // strip away account name
            {
                x_account = host_x.Substring(pos + 1);
                host_x = host_x.Substring(0, pos);
            }

            if (x_host.Length == 0)
                x_host = host_x;
            else
                x_nick = host_x;

            header = header.ToLower();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            MethodInfo mi;
            if (!_packetMethods.TryGetValue(header, out mi))
            {
                System.IO.File.AppendAllText("datalog.txt", string.Format("Unknown packet header: {0}\r\n", header));
                return false;
            }

            mi.Invoke(this, new object[]
                {
                    new PacketInvocationData(x_host, x_account, x_nick, content)
                });

            return true;
        }

        public override void Process()
        {
            base.Process(); // must be called

            // ...
        }
    }
}

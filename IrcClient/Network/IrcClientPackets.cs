using IrcClient.IRC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClient.Network
{
    public partial class IrcNetworkClient
    {
        public void SendJoinChannel(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new ArgumentException("Channel name must not be empty.");

            if (channelName[0] != '#') // we dont support 'local' channels like &test (see IRC protocol)
                channelName = channelName.Insert(0, "#");

            SendFormat("JOIN {0}\r\n", channelName);
        }

        public void SendChannelChat(IrcChannel channel, string message)
        {
            SendFormat("PRIVMSG {0} :{1}\r\n", channel.Name, message);
        }
    }
}

using IrcClient.IRC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClient.Network
{
    public partial class IrcNetworkClient
    {
        [Packet("001")]
        private void On001(PacketInvocationData pid)
        {
            //Send("JOIN #test\r\n");
            InvokeCallback("ConnectedToServer");
        }

        [Packet("PING")]
        private void OnPing(PacketInvocationData pid)
        {
            SendFormat("PONG {0}\r\n", pid.Content);
        }

        [Packet("JOIN")]
        private void OnJoin(PacketInvocationData pid)
        {
            var channelName = pid.Content.Substring(pid.Content.IndexOf(':') + 1);

            var channel = _channelManager.GetChannel(channelName);
            bool isMyself = string.Compare(pid.Nick, MyNick, true) == 0; // myself

            if (channel == null)
            {
                if (!isMyself) // do not care about JOIN in other channels that we're not in
                    return;

                channel = _channelManager.AddChannel(channelName);
            }

            var user = channel.AddUser(
                AccessLevelHelper.GetAccessLevel(pid.Nick),
                AccessLevelHelper.GetNick(pid.Nick));

            if (isMyself)
                InvokeCallback("MeJoinedChannel", channel);
            else
                InvokeCallback("UserJoinedChannel", channel, user);
        }

        [Packet("PART")]
        private void OnPart(PacketInvocationData pid)
        {
            if (pid.Content.Length == 0)
                return;

            var channelName = pid.Content;
            if (channelName.Contains(' '))
                channelName = channelName.Substring(0, channelName.IndexOf(' '));

            var channel = _channelManager.GetChannel(channelName);
            if (channel == null)
                throw new InvalidOperationException("Unknown channel: " + channelName);

            var user = channel.GetUser(AccessLevelHelper.GetNick(pid.Nick));
            if (user != null)
            {
                if (!channel.RemoveUser(AccessLevelHelper.GetNick(pid.Nick)))
                    throw new InvalidOperationException("Failed to remove user from channel");
                InvokeCallback("UserLeftChannel", channel, user);
            }
        }

        [Packet("353")]
        private void On353(PacketInvocationData pid) // userlist
        {
            var x_data = pid.Content;
            int pos;

            if ((pos = x_data.IndexOf(" = ")) == -1 && // '=' is public channel
                (pos = x_data.IndexOf(" * ")) == -1 && // '*' is private channel
                (pos = x_data.IndexOf(" @ ")) == -1)   // '@' is secret channel
                throw new Exception("Malformed userlist received");

            x_data = x_data.Substring(pos + 3); // #test :Hello Test2 Nice

            if ((pos = x_data.IndexOf(" :")) == -1)
                throw new Exception("Malformed userlist received");

            var channelName = x_data.Substring(0, pos);
            if (channelName.Length < 2)
                throw new Exception("Malformed userlist received");

            var channel = _channelManager.GetChannel(channelName);
            if (channel == null)
                throw new Exception("Unknown channel in 353 packet: " + channelName);

            x_data = x_data.Substring(pos + 2);
            if (!x_data.EndsWith(" ")) // otherwise we dont get the last name ...
                x_data = x_data + ' ';
            while ((pos = x_data.IndexOf(' ')) != -1)
            {
                var rawnick = x_data.Substring(0, pos);
                x_data = x_data.Substring(pos + 1);

                string nick;
                AccessLevel access;
                AccessLevelHelper.Strip(rawnick, out nick, out access);

                if (nick.Length > 0 &&
                    string.Compare(nick, MyNick, true) != 0)
                {
                    Debug.WriteLine(string.Format("AddUser '{0}' in channel '{1}'", nick, channelName));
                    IrcChannelUser user;
                    if ((user = channel.AddUser(access, nick)) != null)
                        InvokeCallback("UserJoinedChannel", channel, user);
                }
            }
        }

        [Packet("366")]
        private void On366(PacketInvocationData pid) // end of userlist
        {
        }

        [Packet("PRIVMSG")]
        private void OnPrivateMessage(PacketInvocationData pid)
        {
            var msg = pid.Content;
            int pos;

            if ((pos = msg.IndexOf(" :")) == -1)
                throw new Exception("Malformed PRIVMSG");

            var channelName = msg.Substring(0, pos);
            msg = msg.Substring(pos + 2);

            if (channelName.Length < 2)
                throw new Exception("Malformed PRIVMSG");

            var channel = _channelManager.GetChannel(channelName);
            if (channel == null)
                throw new Exception("PRIVMSG - Channel not found: " + channelName);

            var user = channel.GetUser(AccessLevelHelper.GetNick(pid.Nick));
            if (user == null)
                throw new Exception("PRIVMSG - User '" + AccessLevelHelper.GetNick(pid.Nick) + "' not found in channel: " + channelName);

            InvokeCallback("ChannelMessage", channel, user, msg);
        }

        [Packet("332")]
        private void On332(PacketInvocationData pid) // topic
        {

        }
    }

    #region PacketAttribute and PacketInvocationData
    [AttributeUsage(AttributeTargets.Method)]
    public class PacketAttribute : Attribute
    {
        public string Header { get; private set; }

        public PacketAttribute(string header)
        {
            Header = header.ToLower();
        }
    }

    public class PacketInvocationData
    {
        /// <summary>
        /// Host (x_host)
        /// </summary>
        public string Host { get; private set; }
        /// <summary>
        /// Account (x_account)
        /// </summary>
        public string Account { get; private set; }
        /// <summary>
        /// Nickname (x_nick) - This is the RAW nickname (includes prefix!)
        /// </summary>
        public string Nick { get; private set; }
        /// <summary>
        /// The content of the packet.
        /// </summary>
        public string Content { get; private set; }

        public PacketInvocationData(string host, string account, string nick, string content)
        {
            Host = host;
            Account = account;
            Nick = nick;
            Content = content;
        }
    }
    #endregion
}

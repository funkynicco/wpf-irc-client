using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClient.IRC
{
    #region Access Level
    /*
    ~ for owners – to get this, you need to be +q in the channel
    & for admins – to get this, you need to be +a in the channel
    @ for full operators – to get this, you need to be +o in the channel
    % for half operators – to get this, you need to be +h in the channel
    + for voiced users – to get this, you need to be +v in the channel
    Users with no status in the channel will have no nick prefix
    */
    public enum AccessLevel : byte
    {
        /// <summary>
        /// Prefix: ~
        /// </summary>
        Owner = (byte)'~',
        /// <summary>
        /// Prefix: &
        /// </summary>
        Admin = (byte)'&',
        /// <summary>
        /// Prefix: @
        /// </summary>
        FullOperator = (byte)'@',
        /// <summary>
        /// Prefix: %
        /// </summary>
        HalfOperator = (byte)'%',
        /// <summary>
        /// Prefix: +
        /// </summary>
        Voiced = (byte)'+',
        /// <summary>
        /// Represents a normal user.
        /// </summary>
        Normal = 0
    }

    public static class AccessLevelHelper
    {
        /// <summary>
        /// Removes any access level prefix of a nick.
        /// </summary>
        public static string GetNick(string rawnick)
        {
            if (GetAccessLevel(rawnick) != AccessLevel.Normal)
                return rawnick.Substring(1);

            return rawnick;
        }

        /// <summary>
        /// Tries to parse a nick and determine the access level based on the prefix.
        /// </summary>
        public static AccessLevel GetAccessLevel(string rawnick)
        {
            if (rawnick.Length == 0)
                return AccessLevel.Normal;

            switch (rawnick[0])
            {
                case '~':
                case '&':
                case '@':
                case '%':
                case '+':
                    return (AccessLevel)(byte)rawnick[0];
            }

            return AccessLevel.Normal;
        }

        public static void Strip(string rawnick, out string nick, out AccessLevel access)
        {
            if (string.IsNullOrWhiteSpace(rawnick))
                throw new ArgumentException("rawnick parameter cannot be empty.");

            nick = GetNick(rawnick);
            access = GetAccessLevel(rawnick);
        }

        public static string GetDecoratedNick(AccessLevel access, string nick)
        {
            if (access == AccessLevel.Normal)
                return nick;

            return string.Format("{0}{1}", (char)access, GetNick(nick));
        }
    }
    #endregion

    public class IrcChannelUser
    {
        public AccessLevel Access { get; private set; }
        public string Nick { get; private set; }
        //public bool IsMe { get; private set; }

        public IrcChannelUser(AccessLevel access, string nick)
        {
            Access = access;
            Nick = nick;
        }
    }

    public class IrcChannel : IEnumerable<IrcChannelUser>
    {
        public string Name { get; private set; }
        private string _topic = string.Empty;
        public string Topic
        {
            get { return _topic; }
            set
            {
                _topic = value;
                if (_topic == null)
                    _topic = string.Empty;
            }
        }

        private readonly Dictionary<string, IrcChannelUser> _users = new Dictionary<string, IrcChannelUser>();

        public IrcChannel(string name)
        {
            Name = name;
            Topic = string.Empty;
        }

        public IrcChannelUser AddUser(AccessLevel access, string nick)
        {
            if (IsMember(nick))
                return _users[nick.ToLower()];

            var user = new IrcChannelUser(access, nick);
            _users.Add(nick.ToLower(), user);
            return user;
        }

        public bool RemoveUser(string nick)
        {
            return _users.Remove(nick.ToLower());
        }

        public bool IsMember(string nick)
        {
            return _users.ContainsKey(nick.ToLower());
        }

        public IrcChannelUser GetUser(string nick)
        {
            IrcChannelUser user = null;
            _users.TryGetValue(nick.ToLower(), out user);
            return user;
        }

        public IEnumerator<IrcChannelUser> GetEnumerator()
        {
            return _users.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _users.Values.GetEnumerator();
        }
    }

    public class ChannelManager : IEnumerable<IrcChannel>
    {
        private readonly Dictionary<string, IrcChannel> _channels = new Dictionary<string, IrcChannel>();

        public ChannelManager()
        {
        }

        public IrcChannel AddChannel(string name)
        {
            var channel = GetChannel(name);
            if (channel != null)
                return channel;

            channel = new IrcChannel(name);
            _channels.Add(name.ToLower(), channel);
            return channel;
        }

        public bool RemoveChannel(string name)
        {
            return _channels.Remove(name.ToLower());
        }

        public IrcChannel GetChannel(string name)
        {
            IrcChannel channel;
            if (!_channels.TryGetValue(name.ToLower(), out channel))
                return null;

            return channel;
        }

        public IEnumerator<IrcChannel> GetEnumerator()
        {
            return _channels.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _channels.Values.GetEnumerator();
        }
    }
}

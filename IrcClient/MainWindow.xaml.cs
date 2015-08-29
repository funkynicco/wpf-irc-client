using IrcClient.IRC;
using IrcClient.Network;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace IrcClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IrcNetworkClient _client = new IrcNetworkClient(Configuration.Nick);
        private readonly Queue<string> _joinChannelsQueue = new Queue<string>();
        private bool _isJoiningChannel = false;
        private bool _isFullyConnected = false;
        private int _nextJoinTick = 0;

        public MainWindow()
        {
            InitializeComponent();

            RegisterIrcEvents();

            // add channels to the join channel queue
            if (Configuration.AutoJoinSavedChannels)
            {
                using (var key = Registry.CurrentUser.OpenSubKey(Configuration.RegistryKey + @"\Channels"))
                {
                    if (key != null)
                    {
                        foreach (var channelName in key.GetSubKeyNames())
                        {
                            using (var channelKey = key.OpenSubKey(channelName))
                            {
                                if (channelKey != null)
                                {
                                    var disabled = (int)channelKey.GetValue("Disabled", 0) != 0;
                                    if (!disabled)
                                        _joinChannelsQueue.Enqueue(channelName);
                                }
                            }
                        }
                    }
                }
            }

            Loaded += (sender, e) =>
            {
                _client.Connect(Configuration.Address, Configuration.Port);

                new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(100), IsEnabled = true }.Tick += (_sender, _e) =>
                        {
                            (_sender as DispatcherTimer).IsEnabled = false;
                            _client.Process();

                            var tick = Environment.TickCount;

                            // check joining queue
                            if (tick >= _nextJoinTick &&
                                _isFullyConnected &&
                                !_isJoiningChannel &&
                                _joinChannelsQueue.Count > 0)
                            {
                                _isJoiningChannel = true;
                                _client.SendJoinChannel(_joinChannelsQueue.Peek());
                            }

                            (_sender as DispatcherTimer).IsEnabled = true;
                        };
            };

            ui_TabControl.SelectionChanged += Ui_TabControl_SelectionChanged;

            txtChatMessage.KeyDown += (sender, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        var command = txtChatMessage.Text;
                        txtChatMessage.Text = "";

                        if (command.Length > 0)
                        {
                            if (command[0] == '/')
                            {
                                command = command.Substring(1);

                                // command
                                var content = string.Empty;
                                int pos;
                                if ((pos = command.IndexOf(' ')) != -1)
                                {
                                    content = command.Substring(pos + 1);
                                    command = command.Substring(0, pos);
                                }

                                OnClientCommand(command.ToLower(), content);
                            }
                            else
                            {
                                var channel = GetSelectedChannel();
                                if (channel != null)
                                {
                                    _client.SendChannelChat(channel, command);
                                    AddChatMessage(DateTime.Now, channel, channel.GetUser(_client.MyNick), command, true);
                                }
                            }
                        }
                    }
                };
        }

        private void Ui_TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ui_UserList.Items.Clear();
            var channel = GetSelectedChannel();
            if (channel != null)
            {
                foreach (var user in channel)
                {
                    ui_UserList.Items.Add(user.Nick);
                }
            }
        }

        private void OnClientCommand(string command, string content)
        {
            if (command == "join")
            {
                if (content.Length == 0)
                    return;

                //_client.SendJoinChannel(content);
                _joinChannelsQueue.Enqueue(content);
            }
            else if (command == "clear")
            {
                if (ui_TabControl.SelectedItem != null)
                {
                    var rtb = (RichTextBox)((TabItem)ui_TabControl.SelectedItem).Content;
                    rtb.Document.Blocks.Clear();
                }
            }
        }

        private IrcChannel GetSelectedChannel()
        {
            if (ui_TabControl.SelectedItem != null)
            {
                var channelName = ((TabItem)ui_TabControl.SelectedItem).Header.ToString();

                return _client.Channels.GetChannel(channelName);
            }

            return null;
        }

        private void RegisterIrcEvents()
        {
            _client.RegisterCallback<ConnectedToServerEvent>("ConnectedToServer", () =>
                {
                    // we can now join channels
                    _nextJoinTick = Environment.TickCount + 1000;
                    _isFullyConnected = true;
                });

            _client.RegisterCallback<MeJoinedChannelEvent>("MeJoinedChannel", (channel) =>
                {
                    var richTextBox = new RichTextBox(new FlowDocument())
                    {
                        Background = new SolidColorBrush(Color.FromArgb(0xff, 0x25, 0x25, 0x25)),
                        Foreground = new SolidColorBrush(Color.FromArgb(0xff, 0xb9, 0xb9, 0xb9)),
                        IsReadOnly = true,
                        AcceptsReturn = false,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    };

                    var tabItem = new TabItem();
                    tabItem.Header = channel.Name;
                    tabItem.Content = richTextBox;

                    ui_TabControl.Items.Add(tabItem);

                    //ui_UserList.Items.Clear();
                    //ui_UserList.Items.Add(_client.MyNick);

                    //ui_UserList.SelectedIndex = ui_UserList.Items.Count - 1;
                    ui_TabControl.SelectedIndex = ui_TabControl.Items.Count - 1;

                    if (_joinChannelsQueue.Count > 0 &&
                        _isJoiningChannel &&
                        channel.Name == _joinChannelsQueue.Peek())
                    {
                        _nextJoinTick = Environment.TickCount + 1000; // give it 1 second at least between joining channels
                        _joinChannelsQueue.Dequeue();
                        _isJoiningChannel = false; // process next
                    }
                });

            _client.RegisterCallback<MeLeftChannelEvent>("MeLeftChannel", (channel) =>
                {
                    foreach (var tabobj in ui_TabControl.Items)
                    {
                        var tab = (TabItem)tabobj;
                        if (string.Compare((string)tab.Header, channel.Name) == 0)
                        {
                            ui_TabControl.Items.Remove(tabobj);
                            break;
                        }
                    }

                    // select adjacent (next) tab
                });

            _client.RegisterCallback<UserJoinedChannelEvent>("UserJoinedChannel", (channel, user) =>
                {
                    // TODO: check if its selected tab..
                    ui_UserList.Items.Add(user.Nick);
                });

            _client.RegisterCallback<UserLeftChannelEvent>("UserLeftChannel", (channel, user) =>
                {
                    // TODO: check if its selected tab..
                    ui_UserList.Items.Remove(user.Nick);
                });

            _client.RegisterCallback<ChannelMessageEvent>("ChannelMessage", (channel, user, message) =>
                {
                    AddChatMessage(DateTime.Now, channel, user, message, true);
                });
        }

        public void AddChatMessage(DateTime date, IrcChannel channel, IrcChannelUser user, string message, bool addSender)
        {
            RichTextBox chat = null;

            foreach (var tabobj in ui_TabControl.Items)
            {
                if (string.Compare(((TabItem)tabobj).Header.ToString(), channel.Name) == 0)
                {
                    chat = (RichTextBox)((TabItem)tabobj).Content;
                    break;
                }
            }

            var flowDocument = chat.Document;

            // #1585b5
            Brush nameColor = new SolidColorBrush(Color.FromArgb(0xff, 0x15, 0x85, 0xb5)); ;
            if (AccessLevelHelper.GetNick(user.Nick) == Configuration.Nick)
                nameColor = new SolidColorBrush(Colors.Orange);

            if (flowDocument.Blocks.Count > 0)
            {
                if (Configuration.LineSplitMessages ||
                    addSender)
                {
                    var panel = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Margin = new Thickness(0, 3, 0, 0)
                    };
                    panel.Children.Add(new Rectangle()
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Fill = new SolidColorBrush(Color.FromArgb(0xff, 70, 70, 70)),
                        Height = 1
                    });
                    flowDocument.Blocks.Add(new BlockUIContainer(panel));
                }
                else
                    flowDocument.Blocks.Add(new BlockUIContainer(new StackPanel()
                    {
                        Margin = new Thickness(0, 5, 0, 0)
                    }));
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) }); // nick
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }); // chat message
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) }); // time stamp

            var textBlock = new TextBlock()
            {
                Text = addSender ? AccessLevelHelper.GetNick(user.Nick) : "",
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 10, 0),
                TextWrapping = TextWrapping.Wrap
            };
            if (nameColor != null)
                textBlock.Foreground = nameColor;
            Grid.SetColumn(textBlock, 0);
            grid.Children.Add(textBlock);

            textBlock = new TextBlock() { Text = message, TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(textBlock, 1);
            grid.Children.Add(textBlock);

            textBlock = new TextBlock()
            {
                Text = date.ToString("HH:mm:ss"),
                HorizontalAlignment = HorizontalAlignment.Right,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(textBlock, 2);
            grid.Children.Add(textBlock);

            flowDocument.Blocks.Add(new BlockUIContainer(grid));

            chat.ScrollToEnd();
        }
    }
}

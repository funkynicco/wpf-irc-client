using IrcClient.IRC;
using IrcClient.Network;
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
        private readonly IrcNetworkClient _client = new IrcNetworkClient();

        public MainWindow()
        {
            InitializeComponent();

            var dt = DateTime.Now;
            AddChatMessage((dt = dt.AddSeconds(5)), "Nicco", "abc", true);
            AddChatMessage((dt = dt.AddSeconds(5)), "John", "123", true);
            AddChatMessage((dt = dt.AddSeconds(5)), "Nicco", "nope", true);
            AddChatMessage((dt = dt.AddSeconds(5)), "John", "ok", true);

            RegisterIrcEvents();

            Loaded += (sender, e) =>
            {
                _client.Connect("candice.local", 6667);

                new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(100), IsEnabled = true }.Tick += (_sender, _e) =>
                        {
                            (_sender as DispatcherTimer).IsEnabled = false;
                            _client.Process();
                            (_sender as DispatcherTimer).IsEnabled = true;
                        };
            };
        }

        private void RegisterIrcEvents()
        {
            _client.RegisterCallback<MeJoinedChannelEvent>("MeJoinedChannel", (channel) =>
                {
                    // add channel to UI etc..

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

                    ui_UserList.Items.Clear();
                    ui_UserList.Items.Add(IrcNetworkClient.MyNick);
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

                    // select another tab
                });

            _client.RegisterCallback<UserJoinedChannelEvent>("UserJoinedChannel", (channel, user) =>
                {
                    ui_UserList.Items.Add(user.Nick);
                });

            _client.RegisterCallback<UserLeftChannelEvent>("UserLeftChannel", (channel, user) =>
                {
                    ui_UserList.Items.Remove(user.Nick);
                });

            _client.RegisterCallback<ChannelMessageEvent>("ChannelMessage", (channel, user, message) =>
                {
                    AddChatMessage(DateTime.Now, user.Nick, message, true);
                });
        }

        public void AddChatMessage(DateTime date, string sender, string message, bool addSender)
        {
            // TEMP
            if (ui_TabControl.SelectedIndex == -1)
                return;

            var chat = (RichTextBox)ui_TabControl.SelectedContent;
            var flowDocument = chat.Document;

            //FlowDocument flowDocument = ui_TabControl.SelectedIndex!=-1?(FlowDocument)

            // #1585b5
            Brush nameColor = new SolidColorBrush(Color.FromArgb(0xff, 0x15, 0x85, 0xb5)); ;
            if (sender == Configuration.MyNick)
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
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
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
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });

            var textBlock = new TextBlock()
            {
                Text = addSender ? sender : "",
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
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
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(textBlock, 2);
            grid.Children.Add(textBlock);

            flowDocument.Blocks.Add(new BlockUIContainer(grid));

            chat.ScrollToEnd();
        }
    }
}

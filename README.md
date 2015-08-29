**Very simple WPF based IRC client.**

Only allows the absolute basic chatting capability, but does so in an intuitive WPF design which many other IRC clients lack.

It doesn't support multiple IRC servers, it's primary intention is to use with one network (such as FreeNode). This is however only a UI restriction, the network layer code are object instance based so multiple clients can be run simultaneously.

The *nick*, *server address* and *port* are retrieved from the registry.
Configuration for automatically joining channels are also defined in the registry.

[See the wiki here for configurating for first run](https://bitbucket.org/funkynicco/wpf-irc-client/wiki/Configuration)

![irc.png](https://bitbucket.org/repo/RpKan9/images/794983239-irc.png)
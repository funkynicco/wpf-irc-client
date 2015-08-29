**Very simple WPF based IRC client.**

Only allows the absolute basic chatting capability, but does so in an intuitive WPF design which many other IRC clients lack.

It doesn't support multiple IRC servers, it's primary intention is to use with one network (such as FreeNode). This is however only a UI restriction, the network layer code are object instance based so multiple clients can be run simultaneously.

The source code must be edited to setup configurations for now (your nick, address/port, automatic joining of channels).
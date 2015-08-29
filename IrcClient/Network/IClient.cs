namespace IrcClient.Network
{
    public interface IClient
    {
        int Send(byte[] data, int length);
    }
}

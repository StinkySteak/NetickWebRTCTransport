namespace Netick.Transport.WebRTC
{
    internal enum DisconnectReason
    {
        SignalingServerUnreachable,
        Timeout,
        Shutdown,
        ConnectionClosed,
        ConnectionRejected
    }
}

using System;

namespace Netick.Transport.WebRTC
{
    internal interface IWebRTCNetEventListener
    {
        void OnPeerConnected(WebRTCPeer peer);
        void OnPeerDisconnected(WebRTCPeer peer, DisconnectReason reason);
        void OnNetworkReceive(WebRTCPeer peer, byte[] bytes);
        void OnMessageReceiveUnmanaged(WebRTCPeer peer, IntPtr ptr, int length);
    }
}

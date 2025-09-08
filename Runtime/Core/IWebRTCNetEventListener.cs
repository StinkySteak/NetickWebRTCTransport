using System;

namespace Netick.Transport.WebRTC
{
    internal interface IWebRTCNetEventListener
    {
        void OnPeerConnected(BaseWebRTCPeer peer);
        void OnPeerDisconnected(BaseWebRTCPeer peer, DisconnectReason reason);
        void OnNetworkReceive(BaseWebRTCPeer peer, byte[] bytes, int length);
    }
}

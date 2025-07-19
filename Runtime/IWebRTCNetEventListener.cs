using System;
using UnityEngine;

namespace StinkySteak.N2D
{
    public interface IWebRTCNetEventListener
    {
        void OnPeerConnected(WebRTCPeer peer);
        void OnPeerDisconnected(WebRTCPeer peer);
        void OnNetworkReceive(WebRTCPeer peer, byte[] bytes);
        void OnMessageReceiveUnmanaged(WebRTCPeer peer, IntPtr ptr, int length);
    }
}

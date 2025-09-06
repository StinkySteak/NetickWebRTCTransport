using Netick.Transport.WebRTC;
using StinkySteak.Timer;
using System;
using Unity.WebRTC;

namespace Netick.Transport
{
    public abstract class BaseWebRTCPeer
    {
        public event Action<BaseWebRTCPeer, byte[]> OnMessageReceived;
        public event Action<BaseWebRTCPeer, IntPtr, int> OnMessageReceivedUnmanaged;

        public event Action<BaseWebRTCPeer> OnConnectionClosed;
        public event Action<BaseWebRTCPeer> OnTimeout;

        public abstract IEndPoint EndPoint { get; }
        public abstract bool IsConnectionOpen { get; }
        public abstract bool IsTimedOut { get; }

        //  public abstract void SetConfig(UserRTCConfig userRTCConfig, WebSocketSignalingConfig webSocketSignalingConfig);

        public abstract void Send(IntPtr ptr, int length, bool isReliable);

        public abstract void Connect(string address, int port);

        public abstract void CloseConnection();

        public abstract void Start(RunMode runMode);

        public abstract void PollUpdate();

       // public abstract void SetSignalingServer(WebSocketSignalingServer signalingServer);

        public abstract void OnReceivedOfferFromClient(string offer);

        public abstract void SetConnectionId(int id);

        internal abstract void Init(NetickEngine engine, SignalingWebClient signalingWebClient, UserRTCConfig userRTCConfig);

        public abstract void SetToJoinCode(string toJoinCode);

        public abstract void StartAndOffer();

        public abstract void SetFromOfferConnectionId(int fromConnectionId);

        public abstract void StartFromOffer(RTCSessionDescription sdp);

        protected void BroadcastOnMessage(byte[] bytes)
        {
            OnMessageReceived?.Invoke(this, bytes);
        }

        protected void BroadcastOnMessageUnmanaged(IntPtr ptr, int length)
        {
            OnMessageReceivedUnmanaged?.Invoke(this, ptr, length);
        }

        protected void BroadcastOnTimeout()
        {
            OnTimeout?.Invoke(this);
        }

        protected void BroadcastOnConnectionClosed()
        {
            OnConnectionClosed?.Invoke(this);
        }
    }
}

using Netick.Transport.WebRTC;
using System;
using Unity.WebRTC;
using UnityEngine;

namespace Netick.Transport
{
    public class BrowserWebRTCPeer : BaseWebRTCPeer
    {
        public override IEndPoint EndPoint => throw new NotImplementedException();

        public override bool IsConnectionOpen => throw new NotImplementedException();

        public override bool IsTimedOut => throw new NotImplementedException();

        public override void CloseConnection()
        {
            throw new NotImplementedException();
        }

        public override void Connect(string address, int port)
        {
            throw new NotImplementedException();
        }

        public override void OnReceivedOfferFromClient(string offer)
        {
            throw new NotImplementedException();
        }

        public override void PollUpdate()
        {
            throw new NotImplementedException();
        }

        public override void Send(IntPtr ptr, int length, bool isReliable)
        {
            throw new NotImplementedException();
        }

        public override void SetConnectionId(int id)
        {
            throw new NotImplementedException();
        }

        public override void SetFromOfferConnectionId(int fromConnectionId)
        {
            throw new NotImplementedException();
        }

        public override void SetToJoinCode(string toJoinCode)
        {
            throw new NotImplementedException();
        }

        public override void Start(RunMode runMode)
        {
            throw new NotImplementedException();
        }

        public override void StartAndOffer()
        {
            throw new NotImplementedException();
        }

        public override void StartFromOffer(RTCSessionDescription sdp)
        {
            throw new NotImplementedException();
        }

        internal override void Init(NetickEngine engine, SignalingWebClient signalingWebClient, UserRTCConfig userRTCConfig)
        {
            throw new NotImplementedException();
        }
    }
}

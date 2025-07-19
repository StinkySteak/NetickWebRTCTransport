using Netick;
using StinkySteak.Timer;
using System;
using Unity.WebRTC;
using UnityEngine;

namespace StinkySteak.N2D
{
    public class WebRTCPeer
    {
        private NetickEngine _engine;
        private SignalingWebClient _signalingWebClient;

        private WebRTCEndPoint _endPoint = new();
        private RTCPeerConnection _peerConnection;
        private RTCDataChannel _dataChannel;

        private RTCSessionDescriptionAsyncOperation _opCreateOffer;
        private RTCSessionDescriptionAsyncOperation _opCreateAnswer;
        private RTCSetSessionDescriptionAsyncOperation _opSetLocalDesc;
        private RTCSetSessionDescriptionAsyncOperation _opSetRemoteDesc;

        private float _iceCandidateGatheringDuration = 1f;
        private FlexTimer _timerIceCandidateGathering;

        private string _toJoinCode;
        private int _offerererConnectionId;

        public event Action OnSendChannelOpen;
        public event Action OnSendChannelClosed;

        public WebRTCEndPoint EndPoint => _endPoint;
        public bool IsConnectionOpen => _dataChannel != null && _dataChannel.ReadyState == RTCDataChannelState.Open;

        public void Init(NetickEngine engine, SignalingWebClient signalingWebClient)
        {
            _engine = engine;
            _signalingWebClient = signalingWebClient;
            _signalingWebClient.OnMessageAnswer += OnMessageAnswer;
        }

        public void CloseConnection()
        {
            _peerConnection?.Close();
            _dataChannel?.Close();
        }

        private void OnMessageAnswer(SignalingMessageAnswer message)
        {
            Debug.Log($"[{nameof(WebRTCPeer)}]: Applying answer as remote description...");

            RTCSessionDescription sdp = message.Answer;

            _opSetRemoteDesc = _peerConnection.SetRemoteDescription(ref sdp);
        }

        public void SetToJoinCode(string toJoinCode)
        {
            _toJoinCode = toJoinCode;
        }

        public void SetFromOfferConnectionId(int fromConnectionId)
        {
            _offerererConnectionId = fromConnectionId;
        }

        public void StartFromOffer(RTCSessionDescription sdp)
        {
            _peerConnection = new RTCPeerConnection();
            _peerConnection.OnDataChannel = OnDataChannelCreated;
            _peerConnection.OnIceCandidate = OnIceCandidate;
            _peerConnection.OnIceConnectionChange = OnIceConnectionChanged;
            _peerConnection.OnConnectionStateChange = OnConnectionStateChanged;

            _opSetRemoteDesc = _peerConnection.SetRemoteDescription(ref sdp);

            Debug.Log($"[{nameof(WebRTCPeer)}]: Applying offer as remote description...");
        }

        public void Send(IntPtr ptr, int length)
        {
            _dataChannel.Send(ptr, length);
        }

        private void OnConnectionStateChanged(RTCPeerConnectionState connectionState)
        {
            Debug.Log($"[{nameof(WebRTCPeer)}]: connectionState changed: {connectionState}");
        }

        private void OnIceConnectionChanged(RTCIceConnectionState iceConnectionState)
        {
            Debug.Log($"[{nameof(WebRTCPeer)}]: iceConnection changed: {iceConnectionState}");
        }

        private void OnIceCandidate(RTCIceCandidate iceCandidate)
        {
            Debug.Log($"[{nameof(WebRTCPeer)}]: on Ice Candidate: {iceCandidate.Candidate}");
        }

        private void OnDataChannelCreated(RTCDataChannel dataChannel)
        {
            _dataChannel = dataChannel;

            _dataChannel.OnOpen = OnDataChannelOpen;
            _dataChannel.OnClose = OnDataChannelClose;
            _dataChannel.OnMessage = OnDataChannelMessage;

            Debug.Log($"[{nameof(WebRTCPeer)}]: On Data channel created!");
        }

        public event Action<WebRTCPeer, byte[]> OnMessageReceived;

        private void OnDataChannelMessage(byte[] bytes)
        {
            OnMessageReceived?.Invoke(this, bytes);
        }

        public void StartAndOffer()
        {
            _peerConnection = new RTCPeerConnection();
            _peerConnection.OnIceCandidate = OnIceCandidate;
            _peerConnection.OnIceConnectionChange = OnIceConnectionChanged;
            _peerConnection.OnConnectionStateChange = OnConnectionStateChanged;

            _dataChannel = _peerConnection.CreateDataChannel("sendChannel", new RTCDataChannelInit() { maxRetransmits = 0, ordered = false });
            _dataChannel.OnOpen = OnDataChannelOpen;
            _dataChannel.OnClose = OnDataChannelClose;
            _dataChannel.OnMessage = OnDataChannelMessage;

            _opCreateOffer = _peerConnection.CreateOffer();

            Debug.Log($"[{nameof(WebRTCPeer)}]: Creating offer...");
        }

        private void OnDataChannelOpen()
        {
            SDPParser.ParseSDP(_peerConnection.RemoteDescription.sdp, out string ip, out int port);

            _endPoint.Init(ip, port);

            Debug.Log($"[{nameof(WebRTCPeer)}]: On Data channel open!");
        }

        private void OnDataChannelClose()
        {
            Debug.Log($"[{nameof(WebRTCPeer)}]: On Data channel closed!");
        }

        public void PollUpdate()
        {
            if (_engine.IsServer)
            {
                if (_opSetRemoteDesc != null && _opSetRemoteDesc.IsDone)
                {
                    _opSetRemoteDesc = null;

                    _opCreateAnswer = _peerConnection.CreateAnswer();

                    Debug.Log($"[{nameof(WebRTCPeer)}]: Creating answer...");
                }

                if (_opCreateAnswer != null && _opCreateAnswer.IsDone)
                {
                    RTCSessionDescription sdp = _opCreateAnswer.Desc;
                    _opCreateAnswer = null;
                    Debug.Log($"[{nameof(WebRTCPeer)}]: Answer created. applying as local description....");

                    _opSetLocalDesc = _peerConnection.SetLocalDescription(ref sdp);
                }

                if (_opSetLocalDesc != null && _opSetLocalDesc.IsDone)
                {
                    _opSetLocalDesc = null;
                    Debug.Log($"[{nameof(WebRTCPeer)}]: Gathering ice candidates...");

                    _timerIceCandidateGathering = FlexTimer.CreateFromSeconds(_iceCandidateGatheringDuration);
                }

                if (_timerIceCandidateGathering.IsExpired())
                {
                    _timerIceCandidateGathering = FlexTimer.None;

                    SignalingMessageAnswer message = new SignalingMessageAnswer();
                    message.Type = SignalingMessageType.Answer;
                    message.Answer = _peerConnection.LocalDescription;
                    message.ToConnectionId = _offerererConnectionId;
                    Debug.Log($"[{nameof(WebRTCPeer)}]: Sending answer to remote");

                    _signalingWebClient.Send(message.ToBytes());
                }
            }

            if (_engine.IsClient)
            {
                if (_opCreateOffer != null && _opCreateOffer.IsDone)
                {
                    RTCSessionDescription sdp = _opCreateOffer.Desc;
                    _opCreateOffer = null;

                    _opSetLocalDesc = _peerConnection.SetLocalDescription(ref sdp);

                    Debug.Log($"[{nameof(WebRTCPeer)}]: Applying offer as local description");
                }

                if (_opSetLocalDesc != null && _opSetLocalDesc.IsDone)
                {
                    _opSetLocalDesc = null;

                    _timerIceCandidateGathering = FlexTimer.CreateFromSeconds(_iceCandidateGatheringDuration);

                    Debug.Log($"[{nameof(WebRTCPeer)}]: Applying offer done. gathering candidates...");
                }

                if (_opSetRemoteDesc != null && _opSetRemoteDesc.IsDone)
                {
                    Debug.Log($"[{nameof(WebRTCPeer)}]: Applying answer done. gathering candidates...");

                    _opSetRemoteDesc = null;
                }

                if (_timerIceCandidateGathering.IsExpired())
                {
                    Debug.Log($"[{nameof(WebRTCPeer)}]: Gathering candidates done");

                    _timerIceCandidateGathering = FlexTimer.None;

                    SignalingMessageOffer message = new SignalingMessageOffer();
                    message.Type = SignalingMessageType.Offer;
                    message.Offer = _peerConnection.LocalDescription;
                    message.ToJoinCode = _toJoinCode;

                    Debug.Log($"[{nameof(WebRTCPeer)}]:Sending offer to: {_toJoinCode}");

                    _signalingWebClient.Send(message.ToBytes());
                }
            }
        }
    }
}

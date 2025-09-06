using Newtonsoft.Json;
using StinkySteak.Timer;
using System;
using Unity.WebRTC;
using UnityEngine;

namespace Netick.Transport.WebRTC
{
    public class NativeWebRTCPeer : BaseWebRTCPeer
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

        private FlexTimer _timerIceCandidateGathering;
        private FlexTimer _timerTimeout;

        private UserRTCConfig _userRTCConfig;

        private string _toJoinCode;
        private int _offerererConnectionId;

        private bool _gatherIceCandidates;

        public event Action OnSendChannelOpen;
        public event Action OnSendChannelClosed;

        public override IEndPoint EndPoint => _endPoint;
        public override bool IsConnectionOpen => _dataChannel != null && _dataChannel.ReadyState == RTCDataChannelState.Open;
        public override bool IsTimedOut => _timerTimeout.IsExpired();

        internal override void Init(NetickEngine engine, SignalingWebClient signalingWebClient, UserRTCConfig userRTCConfig)
        {
            _engine = engine;
            _signalingWebClient = signalingWebClient;
            _userRTCConfig = userRTCConfig;
            _timerTimeout = FlexTimer.CreateFromSeconds(_userRTCConfig.RTCTimeoutDuration);

            _signalingWebClient.OnMessageAnswer += OnMessageAnswer;
        }

        public override void CloseConnection()
        {
            _peerConnection?.Close();
            _dataChannel?.Close();
            _timerTimeout = FlexTimer.None;
        }

        private void OnMessageAnswer(SignalingMessageAnswer message)
        {
            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying answer as remote description...");

            RTCSessionDescription sdp = JsonConvert.DeserializeObject<RTCSessionDescription>(message.Answer);

            _opSetRemoteDesc = _peerConnection.SetRemoteDescription(ref sdp);
        }

        public override void SetToJoinCode(string toJoinCode)
        {
            _toJoinCode = toJoinCode;
        }

        public override void SetFromOfferConnectionId(int fromConnectionId)
        {
            _offerererConnectionId = fromConnectionId;
        }

        public override void StartFromOffer(RTCSessionDescription sdp)
        {
            RTCConfiguration configuration = new RTCConfiguration()
            {
                iceServers = _userRTCConfig.IceServers
            };

            _peerConnection = new RTCPeerConnection(ref configuration);
            _peerConnection.OnDataChannel = OnDataChannelCreated;
            _peerConnection.OnIceCandidate = OnIceCandidate;
            _peerConnection.OnIceConnectionChange = OnIceConnectionChanged;
            _peerConnection.OnConnectionStateChange = OnConnectionStateChanged;

            _opSetRemoteDesc = _peerConnection.SetRemoteDescription(ref sdp);

            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying offer as remote description...");
        }

        private void OnConnectionStateChanged(RTCPeerConnectionState connectionState)
        {
            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: connectionState changed: {connectionState}");
        }

        private void OnIceConnectionChanged(RTCIceConnectionState iceConnectionState)
        {
            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: iceConnection changed: {iceConnectionState}");
        }

        private void OnIceCandidate(RTCIceCandidate iceCandidate)
        {
            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: on Ice Candidate: {iceCandidate.Candidate}");
        }

        private void OnDataChannelCreated(RTCDataChannel dataChannel)
        {
            _dataChannel = dataChannel;

            _dataChannel.OnOpen = OnDataChannelOpen;
            _dataChannel.OnClose = OnDataChannelClose;
            _dataChannel.OnMessage = OnDataChannelMessage;

            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: On Data channel created!");
        }


        private void OnDataChannelMessage(byte[] bytes)
        {
            BroadcastOnMessage(bytes);
        }

        public override void StartAndOffer()
        {
            RTCConfiguration configuration = new RTCConfiguration()
            {
                iceServers = _userRTCConfig.IceServers
            };

            _peerConnection = new RTCPeerConnection(ref configuration);
            _peerConnection.OnIceCandidate = OnIceCandidate;
            _peerConnection.OnIceConnectionChange = OnIceConnectionChanged;
            _peerConnection.OnConnectionStateChange = OnConnectionStateChanged;

            _dataChannel = _peerConnection.CreateDataChannel("sendChannel", new RTCDataChannelInit() { maxRetransmits = 0, ordered = false });
            _dataChannel.OnOpen = OnDataChannelOpen;
            _dataChannel.OnClose = OnDataChannelClose;
            _dataChannel.OnMessage = OnDataChannelMessage;

            _opCreateOffer = _peerConnection.CreateOffer();

            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Creating offer...");
        }

        private void OnDataChannelOpen()
        {
            SDPParser.ParseSDP(_peerConnection.RemoteDescription.sdp, out string ip, out int port);

            _endPoint.Init(ip, port);

            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: On Data channel open!");

            _timerTimeout = FlexTimer.None;
        }

        private void OnDataChannelClose()
        {
            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: On Data channel closed!");
            _timerTimeout = FlexTimer.None;
        }

        public override void PollUpdate()
        {
            if (_engine.IsServer)
            {
                if (_opSetRemoteDesc != null && _opSetRemoteDesc.IsDone)
                {
                    _opSetRemoteDesc = null;

                    _opCreateAnswer = _peerConnection.CreateAnswer();

                    Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Creating answer...");
                }

                if (_opCreateAnswer != null && _opCreateAnswer.IsDone)
                {
                    RTCSessionDescription sdp = _opCreateAnswer.Desc;
                    _opCreateAnswer = null;
                    Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Answer created. applying as local description....");

                    _opSetLocalDesc = _peerConnection.SetLocalDescription(ref sdp);
                }

                if (_opSetLocalDesc != null && _opSetLocalDesc.IsDone)
                {
                    _opSetLocalDesc = null;
                    Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Answer has been applied as local description. Gathering ice candidates...");
                    _gatherIceCandidates = true;

                    if (!_userRTCConfig.IceCandidateGatheringConfig.WaitGatheringToComplete)
                        _timerIceCandidateGathering = FlexTimer.CreateFromSeconds(_userRTCConfig.IceCandidateGatheringConfig.GatherDuration);
                }

                if (!_gatherIceCandidates)
                {
                    return;
                }

                if (_timerIceCandidateGathering.IsExpired() || _peerConnection.IceConnectionState == RTCIceConnectionState.Completed)
                {
                    _timerIceCandidateGathering = FlexTimer.None;
                    _gatherIceCandidates = false;

                    SignalingMessageAnswer message = new SignalingMessageAnswer();
                    message.Type = SignalingMessageType.Answer;
                    message.Answer = JsonConvert.SerializeObject(_peerConnection.LocalDescription);
                    message.ToConnectionId = _offerererConnectionId;
                    Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Sending answer to remote");

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

                    Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying offer as local description");
                }

                if (_opSetLocalDesc != null && _opSetLocalDesc.IsDone)
                {
                    _opSetLocalDesc = null;

                    if (!_userRTCConfig.IceCandidateGatheringConfig.WaitGatheringToComplete)
                        _timerIceCandidateGathering = FlexTimer.CreateFromSeconds(_userRTCConfig.IceCandidateGatheringConfig.GatherDuration);

                    _gatherIceCandidates = true;
                    Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying offer done. gathering candidates...");
                }

                if (_opSetRemoteDesc != null && _opSetRemoteDesc.IsDone)
                {
                    Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying answer done. gathering candidates...");

                    _opSetRemoteDesc = null;
                }

                if (!_gatherIceCandidates)
                {
                    return;
                }

                if (_timerIceCandidateGathering.IsExpired() || _peerConnection.IceConnectionState == RTCIceConnectionState.Completed)
                {
                    Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Gathering candidates done");

                    _gatherIceCandidates = false;

                    _timerIceCandidateGathering = FlexTimer.None;

                    SignalingMessageOffer message = new SignalingMessageOffer();
                    message.Type = SignalingMessageType.Offer;
                    message.Offer = JsonConvert.SerializeObject(_peerConnection.LocalDescription);
                    message.ToJoinCode = _toJoinCode;

                    Debug.Log($"[{nameof(NativeWebRTCPeer)}]:Sending offer to: {_toJoinCode}");

                    _signalingWebClient.Send(message.ToBytes());
                }
            }
        }

        public override void Send(IntPtr ptr, int length, bool isReliable)
        {
            _dataChannel.Send(ptr, length);
        }

        public override void Connect(string address, int port)
        {
            throw new NotImplementedException();
        }

        public override void Start(RunMode runMode)
        {
            throw new NotImplementedException();
        }

        public override void OnReceivedOfferFromClient(string offer)
        {
            throw new NotImplementedException();
        }

        public override void SetConnectionId(int id)
        {
            throw new NotImplementedException();
        }
    }
}

#if UNITY_WEBGL && !UNITY_EDITOR
#define BROWSER
#endif

#if !BROWSER
using Unity.WebRTC;
#endif

using Newtonsoft.Json;
using StinkySteak.Timer;
using System;
using UnityEngine;
using StinkySteak.WebRealtimeCommunication;

namespace Netick.Transport.WebRTC
{
    public class NativeWebRTCPeer : BaseWebRTCPeer
    {
#if BROWSER
        public override IEndPoint EndPoint => throw new NotImplementedException();

        public override bool IsConnectionOpen => throw new NotImplementedException();

        public override bool IsTimedOut => throw new NotImplementedException();

        public override void CloseConnection() { }

        public override void PollUpdate() { }

        public override void Send(IntPtr ptr, int length, bool isReliable) { }

        public override void SetFromOfferConnectionId(int fromConnectionId) { }

        public override void SetToJoinCode(string toJoinCode) { }

        public override void Start() { }

        public override void StartAndOffer() { }

        public override void StartFromOffer(WebRTCSessionDescription offer) { }

        internal override void Init(NetickEngine engine, SignalingWebClient signalingWebClient, UserRTCConfig userRTCConfig) { }
#else
        private NetickEngine _engine;
        private SignalingWebClient _signalingWebClient;

        private WebRTCEndPoint _endPoint = new();
        private RTCPeerConnection _peerConnection;
        private RTCDataChannel _dataChannel;
        private RTCDataChannel _dataChannelReliable;

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

        private const string LabelSendChannel = "sendChannel";
        private const string LabelSendChannelReliable = "sendChannelReliable";

        internal override void Init(NetickEngine engine, SignalingWebClient signalingWebClient, UserRTCConfig userRTCConfig)
        {
            _engine = engine;
            _signalingWebClient = signalingWebClient;
            _userRTCConfig = userRTCConfig;

            _signalingWebClient.OnMessageAnswer += OnMessageAnswer;
        }

        public override void Start()
        {
            _timerTimeout = FlexTimer.CreateFromSeconds(_userRTCConfig.RTCTimeoutDuration);
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

            RTCSessionDescription sdp = DenormalizeSDP(message.Answer);

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

        public override void StartFromOffer(WebRTCSessionDescription offer)
        {
            RTCConfiguration configuration = new RTCConfiguration()
            {
                iceServers = GetRTCIceFromUserIce(_userRTCConfig.IceServers)
            };

            RTCSessionDescription sdp = DenormalizeSDP(offer);

            _peerConnection = new RTCPeerConnection(ref configuration);
            _peerConnection.OnDataChannel = OnDataChannelCreated;
            _peerConnection.OnIceCandidate = OnIceCandidate;
            _peerConnection.OnIceConnectionChange = OnIceConnectionChanged;
            _peerConnection.OnConnectionStateChange = OnConnectionStateChanged;

            _opSetRemoteDesc = _peerConnection.SetRemoteDescription(ref sdp);

            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying offer as remote description...");
        }

        protected RTCIceServer[] GetRTCIceFromUserIce(IceServer[] iceServers)
        {
            RTCIceServer[] rtcIceServers = new RTCIceServer[iceServers.Length];

            for (int i = 0; i < iceServers.Length; i++)
            {
                IceServer ice = iceServers[i];

                RTCIceServer rtcIce = new RTCIceServer()
                {
                    credential = ice.Credential,
                    credentialType = RTCIceCredentialType.Password,
                    urls = ice.Urls,
                    username = ice.Username,
                };

                rtcIceServers[i] = rtcIce;
            }

            return rtcIceServers;
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
            if (dataChannel.Label == LabelSendChannel)
            {
                _dataChannel = dataChannel;
                _dataChannel.OnMessage = OnDataChannelMessage;
                _dataChannel.OnClose = OnDataChannelClose;

                SDPParser.ParseSDP(_peerConnection.RemoteDescription.sdp, out string ip, out int port);

                _endPoint.Init(ip, port);
            }
            else if (dataChannel.Label == LabelSendChannelReliable)
            {
                _dataChannelReliable = dataChannel;
                _dataChannelReliable.OnMessage = OnDataChannelReliableMessage;
                _dataChannelReliable.OnClose = OnDataChannelReliableClosed;
            }
        }

        private void OnDataChannelReliableClosed()
        {

        }


        private void OnDataChannelMessage(byte[] bytes)
        {
            BroadcastOnMessage(bytes);
        }

        public override void StartAndOffer()
        {
            RTCConfiguration configuration = new RTCConfiguration()
            {
                iceServers = GetRTCIceFromUserIce(_userRTCConfig.IceServers)
            };

            _peerConnection = new RTCPeerConnection(ref configuration);
            _peerConnection.OnIceCandidate = OnIceCandidate;
            _peerConnection.OnIceConnectionChange = OnIceConnectionChanged;
            _peerConnection.OnConnectionStateChange = OnConnectionStateChanged;

            _dataChannel = _peerConnection.CreateDataChannel(LabelSendChannel, new RTCDataChannelInit() { maxRetransmits = 0, ordered = false });
            _dataChannel.OnOpen = OnDataChannelOpen;
            _dataChannel.OnClose = OnDataChannelClose;
            _dataChannel.OnMessage = OnDataChannelMessage;

            RTCDataChannelInit rtcDataChannelReliableConfig = new RTCDataChannelInit();

            _dataChannelReliable = _peerConnection.CreateDataChannel(LabelSendChannelReliable, rtcDataChannelReliableConfig);
            _dataChannelReliable.OnClose = OnDataChannelReliableClose;
            _dataChannelReliable.OnOpen = OnDataChannelReliableOpen;
            _dataChannelReliable.OnMessage = OnDataChannelReliableMessage;

            _opCreateOffer = _peerConnection.CreateOffer();

            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Creating offer...");
        }

        private void OnDataChannelReliableOpen()
        {
            Debug.Log("OnDataChannelReliableOpen");
        }

        private void OnDataChannelReliableMessage(byte[] bytes)
        {
            Debug.Log("OnDataChannelReliableMessage");
            BroadcastOnMessage(bytes);
        }

        private void OnDataChannelReliableClose()
        {
            Debug.Log("OnDataChannelReliableClose");
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

        private void PollOpSetRemoteOffer()
        {
            if (_opSetRemoteDesc != null && _opSetRemoteDesc.IsDone)
            {
                _opSetRemoteDesc = null;

                _opCreateAnswer = _peerConnection.CreateAnswer();

                Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Creating answer...");
            }
        }
        private void PollOpCreateAnswer()
        {
            if (_opCreateAnswer != null && _opCreateAnswer.IsDone)
            {
                RTCSessionDescription sdp = _opCreateAnswer.Desc;
                _opCreateAnswer = null;
                Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Answer created. applying as local description....");

                _opSetLocalDesc = _peerConnection.SetLocalDescription(ref sdp);
            }
        }

        private void PollOpSetLocalAnswer()
        {
            if (_opSetLocalDesc != null && _opSetLocalDesc.IsDone)
            {
                _opSetLocalDesc = null;
                Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Answer has been applied as local description. Gathering ice candidates...");
                _gatherIceCandidates = true;

                if (!_userRTCConfig.IceCandidateGatheringConfig.WaitGatheringToComplete)
                    _timerIceCandidateGathering = FlexTimer.CreateFromSeconds(_userRTCConfig.IceCandidateGatheringConfig.GatherDuration);
            }
        }

        private void PollGatherIceCandidatesOnServer()
        {
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
                message.Answer = NormalizeSDP(_peerConnection.LocalDescription);
                message.ToConnectionId = _offerererConnectionId;
                Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Sending answer to remote");

                _signalingWebClient.Send(message.ToBytes());
            }
        }

        private void PollOpCreateOffer()
        {
            if (_opCreateOffer != null && _opCreateOffer.IsDone)
            {
                RTCSessionDescription sdp = _opCreateOffer.Desc;
                _opCreateOffer = null;

                _opSetLocalDesc = _peerConnection.SetLocalDescription(ref sdp);

                Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying offer as local description");
            }
        }

        private void PollOpSetLocalOffer()
        {
            if (_opSetLocalDesc != null && _opSetLocalDesc.IsDone)
            {
                _opSetLocalDesc = null;

                if (!_userRTCConfig.IceCandidateGatheringConfig.WaitGatheringToComplete)
                    _timerIceCandidateGathering = FlexTimer.CreateFromSeconds(_userRTCConfig.IceCandidateGatheringConfig.GatherDuration);

                _gatherIceCandidates = true;
                Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying offer done. gathering candidates...");
            }
        }

        private void PollOpSetRemoteAnswer()
        {
            if (_opSetRemoteDesc != null && _opSetRemoteDesc.IsDone)
            {
                Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying answer done. gathering candidates...");

                _opSetRemoteDesc = null;
            }
        }

        private void PollGatherIceCandidatesOnClient()
        {
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
                message.Offer = NormalizeSDP(_peerConnection.LocalDescription);
                message.ToJoinCode = _toJoinCode;

                Debug.Log($"[{nameof(NativeWebRTCPeer)}]:Sending offer to: {_toJoinCode}");

                _signalingWebClient.Send(message.ToBytes());
            }
        }

        private WebRTCSessionDescription NormalizeSDP(RTCSessionDescription sdp)
        {
            string json = JsonConvert.SerializeObject(sdp);
            return JsonConvert.DeserializeObject<WebRTCSessionDescription>(json);
        }

        private RTCSessionDescription DenormalizeSDP(WebRTCSessionDescription sdp)
        {
            string json = JsonConvert.SerializeObject(sdp);
            return JsonConvert.DeserializeObject<RTCSessionDescription>(json);
        }

        public override void PollUpdate()
        {
            if (_engine.IsClient)
            {
                PollOpCreateOffer();
                PollOpSetLocalOffer();
                PollOpSetRemoteAnswer();
                PollGatherIceCandidatesOnClient();
            }

            if (_engine.IsServer)
            {
                PollOpSetRemoteOffer();
                PollOpCreateAnswer();
                PollOpSetLocalAnswer();
                PollGatherIceCandidatesOnServer();
            }
        }

        public override void Send(IntPtr ptr, int length, bool isReliable)
        {
            if (!isReliable)
            {
                _dataChannel.Send(ptr, length);
                return;
            }

            _dataChannelReliable.Send(ptr, length);
        }
#endif
    }
}

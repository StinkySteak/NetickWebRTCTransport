using AOT;
using Netick.Transport.WebRTC;
using StinkySteak.Timer;
using StinkySteak.WebRealtimeCommunication;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netick.Transport
{
    public class BrowserWebRTCPeer : BaseWebRTCPeer
    {
        private static Dictionary<int, BrowserWebRTCPeer> _peers = new Dictionary<int, BrowserWebRTCPeer>();

        private WebRTCEndPoint _endPoint = new();
        private NetickEngine _engine;
        private SignalingWebClient _signalingWebClient;
        private UserRTCConfig _userRTCConfig;
        private FlexTimer _timerIceCandidateGathering;
        private FlexTimer _timerTimeout;
        private int _peerIndex;

        public override IEndPoint EndPoint => _endPoint;
        public override bool IsConnectionOpen => Browser.WebRTC_IsConnectionOpen(_peerIndex);
        public override bool IsTimedOut => _timerTimeout.IsExpired();

        private int _offerererConnectionId;
        private bool _gatherIceCandidates;
        private string _toJoinCode;

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

        private void OnMessageAnswer(SignalingMessageAnswer message)
        {
            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying answer as remote description...");

            Browser.WebRTC_SetRemoteDescription(_peerIndex, message.Answer);
        }

        public override void CloseConnection()
        {
            Browser.WebRTC_CloseConnection(_peerIndex);
            Browser.WebRTC_Reset(_peerIndex);
        }

        public override void PollUpdate()
        {
            if (_engine.IsClient)
            {
                PollOpCreateOffer();
                PollOpSetLocalOffer();
                PollOpSetRemoteAnswer();
                PollIceCandidatesOnClient();
            }

            if (_engine.IsServer)
            {
                PollOpSetRemoteOffer();
                PollOpCreateAnswer();
                PollOpSetLocalAnswer();
                PollGatherIceCandidatesOnServer();
            }
        }

        private void PollOpSetRemoteOffer()
        {
            if (!Browser.WebRTC_HasOpSetRemoteDescription(_peerIndex)) return;

            if (Browser.WebRTC_IsOpSetRemoteDescriptionDone(_peerIndex))
            {
                Debug.Log("Offer has been set to remote description. Creating an answer...");

                Browser.WebRTC_CreateAnswer(_peerIndex);

                Browser.WebRTC_DisposeOpSetRemoteDescription(_peerIndex);
            }
        }

        private void PollOpCreateAnswer()
        {
            if (Browser.WebRTC_GetOpCreateAnswerIsDone(_peerIndex))
            {
                Debug.Log("Answer is created. Setting it as local description...");

                WebRTCSessionDescription answer = Browser.WebRTC_GetAnswer(_peerIndex);

                Browser.WebRTC_SetLocalDescription(_peerIndex, answer);

                Browser.WebRTC_DisposeOpCreateAnswer(_peerIndex);
            }
        }

        private void PollOpSetLocalAnswer()
        {
            if (!Browser.WebRTC_HasOpSetLocalDescription(_peerIndex)) return;

            if (Browser.WebRTC_IsOpSetLocalDescriptionDone(_peerIndex))
            {
                Debug.Log("Answer has been set to local description!");

                if (_userRTCConfig.IceCandidateGatheringConfig.ManualGatheringStop)
                    _timerIceCandidateGathering = FlexTimer.CreateFromSeconds(_userRTCConfig.IceCandidateGatheringConfig.GatherDuration);

                _gatherIceCandidates = true;

                Browser.WebRTC_DisposeOpSetLocalDescription(_peerIndex);
            }
        }

        private void PollOpCreateOffer()
        {
            if (!Browser.WebRTC_HasOpCreateOffer(_peerIndex)) return;

            if (Browser.WebRTC_GetOpCreateOfferIsDone(_peerIndex))
            {
                Debug.Log("Offer Created. Setting it to local...");

                WebRTCSessionDescription offer = Browser.WebRTC_GetOffer(_peerIndex);

                Browser.WebRTC_SetLocalDescription(_peerIndex, offer);

                Browser.WebRTC_DisposeOpCreateOffer(_peerIndex);
            }
        }


        private void PollOpSetLocalOffer()
        {
            if (!Browser.WebRTC_HasOpSetLocalDescription(_peerIndex)) return;

            if (Browser.WebRTC_IsOpSetLocalDescriptionDone(_peerIndex))
            {
                Debug.Log("Offer has been set to local!");

                if (_userRTCConfig.IceCandidateGatheringConfig.ManualGatheringStop)
                    _timerIceCandidateGathering = FlexTimer.CreateFromSeconds(_userRTCConfig.IceCandidateGatheringConfig.GatherDuration);

                _gatherIceCandidates = true;

                Browser.WebRTC_DisposeOpSetLocalDescription(_peerIndex);
            }
        }

        private void PollIceCandidatesOnClient()
        {
            if (!_gatherIceCandidates)
            {
                return;
            }

            if (_timerIceCandidateGathering.IsExpired() || Browser.WebRTC_GetGatheringState(_peerIndex) == BrowserRTCIceGatheringState.Complete)
            {
                _timerIceCandidateGathering = FlexTimer.None;
                _gatherIceCandidates = false;

                WebRTCSessionDescription localSdp = Browser.WebRTC_GetLocalDescription(_peerIndex);

                SignalingMessageOffer message = new SignalingMessageOffer();
                message.Type = SignalingMessageType.Offer;
                message.Offer = localSdp;
                message.ToJoinCode = _toJoinCode;

                Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Sending offer to remote");
                Debug.Log($"[{nameof(NativeWebRTCPeer)}]: localSdp: {localSdp}");

                _signalingWebClient.Send(message.ToBytes());
            }
        }

        private void PollGatherIceCandidatesOnServer()
        {
            if (!_gatherIceCandidates)
            {
                return;
            }

            if (_timerIceCandidateGathering.IsExpired() || Browser.WebRTC_GetGatheringState(_peerIndex) == BrowserRTCIceGatheringState.Complete)
            {
                _timerIceCandidateGathering = FlexTimer.None;
                _gatherIceCandidates = false;

                WebRTCSessionDescription localSdp = Browser.WebRTC_GetLocalDescription(_peerIndex);

                SignalingMessageAnswer message = new SignalingMessageAnswer();
                message.Type = SignalingMessageType.Answer;
                message.Answer = localSdp;
                message.ToConnectionId = _offerererConnectionId;
                Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Sending answer to remote");

                _signalingWebClient.Send(message.ToBytes());
            }
        }

        private void PollOpSetRemoteAnswer()
        {
            if (!Browser.WebRTC_HasOpSetRemoteDescription(_peerIndex)) return;

            if (Browser.WebRTC_IsOpSetRemoteDescriptionDone(_peerIndex))
            {
                Debug.Log("Answer has been set to remote description");

                Browser.WebRTC_DisposeOpSetRemoteDescription(_peerIndex);
            }
        }

        public override void Send(IntPtr ptr, int length, bool isReliable)
        {
            if (!isReliable)
            {
                Browser.WebRTC_DataChannelSend(_peerIndex, ptr, length);
                return;
            }

            Browser.WebRTC_DataChannelReliableSend(_peerIndex, ptr, length);
        }

        public override void SetFromOfferConnectionId(int fromConnectionId)
        {
            _offerererConnectionId = fromConnectionId;
        }

        public override void SetToJoinCode(string toJoinCode)
        {
            _toJoinCode = toJoinCode;
        }

        [MonoPInvokeCallback(typeof(OnMessageCallback))]
        private static void OnDataChannelMessageReceived(int index, IntPtr ptr, int length)
        {
            Debug.Log($"OnDataChannelMessageReceived: {index}");

            BrowserWebRTCPeer peer = _peers[index];
            peer.BroadcastOnMessageUnmanaged(ptr, length);
        }

        [MonoPInvokeCallback(typeof(OnDataChannelOpen))]
        private static void OnDataChannelOpen(int index)
        {
            Debug.Log($"OnDataChannelOpen: {index}");

            BrowserWebRTCPeer peer = _peers[index];
            string remoteDescription = Browser.WebRTC_GetRemoteDescriptionJson(index);

            SDPParser.ParseSDP(remoteDescription, out string ip, out int port);

            UnityEngine.Debug.Log($"remoteSDP: {remoteDescription}");

            peer._timerTimeout = FlexTimer.None;
            peer._endPoint.Init(ip, port);
        }

        [MonoPInvokeCallback(typeof(OnDataChannelCreated))]
        private static void OnDataChannelCreated(int index)
        {
            BrowserWebRTCPeer peer = _peers[index];
            string remoteDescription = Browser.WebRTC_GetRemoteDescriptionJson(index);

            SDPParser.ParseSDP(remoteDescription, out string ip, out int port);

            UnityEngine.Debug.Log($"remoteSDP: {remoteDescription}");

            peer._endPoint.Init(ip, port);
        }

        public override void StartAndOffer()
        {
            BrowserRTCConfiguration configuration = CreateRTCConfiguration();
            BrowserRTCDataChannelInit browserRTCDataChannelInit = new BrowserRTCDataChannelInit();
            browserRTCDataChannelInit.ordered = false;
            browserRTCDataChannelInit.maxRetransmits = 0;

            int index = Browser.WebRTC_CreateRTCPeerConnection(configuration);
            _peerIndex = index;

            _peers.Add(index, this);

            Browser.WebRTC_SetCallbackOnDataChannelCreated(_peerIndex, OnDataChannelCreated);
            Browser.WebRTC_SetCallbackOnDataChannelOpen(_peerIndex, OnDataChannelOpen);
            Browser.WebRTC_CreateDataChannel(_peerIndex, browserRTCDataChannelInit);
            Browser.WebRTC_CreateDataChannelReliable(_peerIndex);
            Browser.WebRTC_SetCallbackOnMessage(_peerIndex, OnDataChannelMessageReceived);

            Browser.WebRTC_CreateOffer(_peerIndex);
        }

        private BrowserRTCConfiguration CreateRTCConfiguration()
        {
            BrowserRTCConfiguration config = default;
            config.iceServers = GetRTCIceFromUserIce(_userRTCConfig.IceServers);

            return config;
        }

        protected BrowserRTCIceServer[] GetRTCIceFromUserIce(IceServer[] iceServers)
        {
            BrowserRTCIceServer[] rtcIceServers = new BrowserRTCIceServer[iceServers.Length];

            for (int i = 0; i < iceServers.Length; i++)
            {
                IceServer ice = iceServers[i];

                BrowserRTCIceServer rtcIce = new BrowserRTCIceServer()
                {
                    credential = ice.Credential,
                    credentialType = BrowserRTCIceCredentialType.Password,
                    urls = ice.Urls,
                    username = ice.Username,
                };

                rtcIceServers[i] = rtcIce;
            }

            return rtcIceServers;
        }

        public override void StartFromOffer(WebRTCSessionDescription offer)
        {
            BrowserRTCConfiguration configuration = CreateRTCConfiguration();

            int index = Browser.WebRTC_CreateRTCPeerConnection(configuration);

            _peerIndex = index;
            _peers.Add(index, this);

            Browser.WebRTC_SetCallbackOnDataChannelCreated(_peerIndex, OnDataChannelCreated);
            Browser.WebRTC_SetCallbackOnMessage(_peerIndex, OnDataChannelMessageReceived);

            Browser.WebRTC_SetRemoteDescription(_peerIndex, offer);
            Browser.WebRTC_CreateOffer(_peerIndex);

            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying offer as remote description...");
        }
    }

    [Serializable]
    public struct IceServer
    {
        public string[] Urls;
        public string Username;
        public string Credential;
    }
}

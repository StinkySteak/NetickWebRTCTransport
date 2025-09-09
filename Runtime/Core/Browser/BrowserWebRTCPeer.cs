using AOT;
using Netick.Transport.WebRTC;
using StinkySteak.Timer;
using StinkySteak.WebRealtimeCommunication;
using System;
using UnityEngine;

namespace Netick.Transport
{
    public class BrowserWebRTCPeer : BaseWebRTCPeer
    {
        private static BrowserWebRTCPeer _instance;

        private WebRTCEndPoint _endPoint = new();
        private NetickEngine _engine;
        private SignalingWebClient _signalingWebClient;
        private UserRTCConfig _userRTCConfig;
        private FlexTimer _timerIceCandidateGathering;
        private FlexTimer _timerTimeout;

        public override IEndPoint EndPoint => _endPoint;
        public override bool IsConnectionOpen => Browser.WebRTC_IsConnectionOpen();
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
            _instance = this;
        }

        public override void Start()
        {
            _timerTimeout = FlexTimer.CreateFromSeconds(_userRTCConfig.RTCTimeoutDuration);
        }

        private void OnMessageAnswer(SignalingMessageAnswer message)
        {
            Debug.Log($"[{nameof(NativeWebRTCPeer)}]: Applying answer as remote description...");

            Browser.WebRTC_SetRemoteDescription(message.Answer);
        }

        public override void CloseConnection()
        {
            Browser.WebRTC_CloseConnection();
            Browser.WebRTC_Reset();
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
            if (!Browser.WebRTC_HasOpSetRemoteDescription()) return;

            if (Browser.WebRTC_IsOpSetRemoteDescriptionDone())
            {
                Debug.Log("Offer has been set to remote description. Creating an answer...");

                Browser.WebRTC_CreateAnswer();

                Browser.WebRTC_DisposeOpSetRemoteDescription();
            }
        }

        private void PollOpCreateAnswer()
        {
            if (Browser.WebRTC_GetOpCreateAnswerIsDone())
            {
                Debug.Log("Answer is created. Setting it as local description...");

                WebRTCSessionDescription answer = Browser.WebRTC_GetAnswer();

                Browser.WebRTC_SetLocalDescription(answer);

                Browser.WebRTC_DisposeOpCreateAnswer();
            }
        }

        private void PollOpSetLocalAnswer()
        {
            if (!Browser.WebRTC_HasOpSetLocalDescription()) return;

            if (Browser.WebRTC_IsOpSetLocalDescriptionDone())
            {
                Debug.Log("Answer has been set to local description!");

                if (_userRTCConfig.IceCandidateGatheringConfig.ManualGatheringStop)
                    _timerIceCandidateGathering = FlexTimer.CreateFromSeconds(_userRTCConfig.IceCandidateGatheringConfig.GatherDuration);

                _gatherIceCandidates = true;

                Browser.WebRTC_DisposeOpSetLocalDescription();
            }
        }

        private void PollOpCreateOffer()
        {
            if (!Browser.WebRTC_HasOpCreateOffer()) return;

            if (Browser.WebRTC_GetOpCreateOfferIsDone())
            {
                Debug.Log("Offer Created. Setting it to local...");

                WebRTCSessionDescription offer = Browser.WebRTC_GetOffer();

                Browser.WebRTC_SetLocalDescription(offer);

                Browser.WebRTC_DisposeOpCreateOffer();
            }
        }


        private void PollOpSetLocalOffer()
        {
            if (!Browser.WebRTC_HasOpSetLocalDescription()) return;

            if (Browser.WebRTC_IsOpSetLocalDescriptionDone())
            {
                Debug.Log("Offer has been set to local!");

                if (_userRTCConfig.IceCandidateGatheringConfig.ManualGatheringStop)
                    _timerIceCandidateGathering = FlexTimer.CreateFromSeconds(_userRTCConfig.IceCandidateGatheringConfig.GatherDuration);

                _gatherIceCandidates = true;

                Browser.WebRTC_DisposeOpSetLocalDescription();
            }
        }

        private void PollIceCandidatesOnClient()
        {
            if (!_gatherIceCandidates)
            {
                return;
            }

            if (_timerIceCandidateGathering.IsExpired() || Browser.WebRTC_GetGatheringState() == BrowserRTCIceGatheringState.Complete)
            {
                _timerIceCandidateGathering = FlexTimer.None;
                _gatherIceCandidates = false;

                WebRTCSessionDescription localSdp = Browser.WebRTC_GetLocalDescription();

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

            if (_timerIceCandidateGathering.IsExpired() || Browser.WebRTC_GetGatheringState() == BrowserRTCIceGatheringState.Complete)
            {
                _timerIceCandidateGathering = FlexTimer.None;
                _gatherIceCandidates = false;

                WebRTCSessionDescription localSdp = Browser.WebRTC_GetLocalDescription();

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
            if (!Browser.WebRTC_HasOpSetRemoteDescription()) return;

            if (Browser.WebRTC_IsOpSetRemoteDescriptionDone())
            {
                Debug.Log("Answer has been set to remote description");

                Browser.WebRTC_DisposeOpSetRemoteDescription();
            }
        }

        public override void Send(IntPtr ptr, int length, bool isReliable)
        {
            if (!isReliable)
            {
                Browser.WebRTC_DataChannelSend(ptr, length);
                return;
            }

            Browser.WebRTC_DataChannelReliableSend(ptr, length);
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
        private static void OnDataChannelMessageReceived(IntPtr ptr, int length)
        {
            _instance.BroadcastOnMessageUnmanaged(ptr, length);
        }

        [MonoPInvokeCallback(typeof(OnDataChannelOpen))]
        private static void OnDataChannelOpen()
        {
            string remoteDescription = Browser.WebRTC_GetRemoteDescriptionJson();

            SDPParser.ParseSDP(remoteDescription, out string ip, out int port);

            UnityEngine.Debug.Log($"remoteSDP: {remoteDescription}");

            _instance._timerTimeout = FlexTimer.None;
            _instance._endPoint.Init(ip, port);
        }

        [MonoPInvokeCallback(typeof(OnDataChannelCreated))]
        private static void OnDataChannelCreated()
        {
            string remoteDescription = Browser.WebRTC_GetRemoteDescriptionJson();

            SDPParser.ParseSDP(remoteDescription, out string ip, out int port);

            UnityEngine.Debug.Log($"remoteSDP: {remoteDescription}");

            _instance._endPoint.Init(ip, port);
        }

        public override void StartAndOffer()
        {
            BrowserRTCConfiguration configuration = CreateRTCConfiguration();
            BrowserRTCDataChannelInit browserRTCDataChannelInit = new BrowserRTCDataChannelInit();
            browserRTCDataChannelInit.ordered = false;
            browserRTCDataChannelInit.maxRetransmits = 0;

            Browser.WebRTC_CreateRTCPeerConnection(configuration);
            Browser.WebRTC_SetCallbackOnDataChannelCreated(OnDataChannelCreated);
            Browser.WebRTC_SetCallbackOnDataChannelOpen(OnDataChannelOpen);
            Browser.WebRTC_CreateDataChannel(browserRTCDataChannelInit);
            Browser.WebRTC_CreateDataChannelReliable();
            Browser.WebRTC_SetCallbackOnMessage(OnDataChannelMessageReceived);

            Browser.WebRTC_CreateOffer();
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

            Browser.WebRTC_CreateRTCPeerConnection(configuration);
            Browser.WebRTC_SetCallbackOnDataChannelCreated(OnDataChannelCreated);
            Browser.WebRTC_SetCallbackOnMessage(OnDataChannelMessageReceived);

            Browser.WebRTC_SetRemoteDescription(offer);
            Browser.WebRTC_CreateOffer();

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

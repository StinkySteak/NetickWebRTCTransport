using System;
using UnityEngine;

namespace StinkySteak.N2D
{
    public class HostAllocationService
    {
        private SignalingWebClient _signalingWebClient;
        private SignalingServerConnectConfig _signalingServerConnectConfig;

        private bool _isJoinCodeAllocated;
        private string _allocatedJoinCode;

        private const float HeartbeatInterval = 5f;
        private float _lastPingTime;

        public string AllocatedJoinCode => _allocatedJoinCode;
        public event Action OnJoinCodeUpdated;

        public void Init(SignalingWebClient signalingWebClient, SignalingServerConnectConfig signalingServerConnectConfig)
        {
            _signalingWebClient = signalingWebClient;
            _signalingServerConnectConfig = signalingServerConnectConfig;
        }

        public void RequestAllocation()
        {
            _signalingWebClient.Connect(_signalingServerConnectConfig);

            _signalingWebClient.OnConnected += OnWebClientConnected;
            _signalingWebClient.OnDisconnected += OnWebClientDisconnected;
            _signalingWebClient.OnMessageJoinCodeAllocated += OnWebClientMessageJoinCodeAllocated;
        }

        public void PollUpdate()
        {
            _signalingWebClient.PollUpdate();

            if (_isJoinCodeAllocated)
            {
                PingHeartbeat();
            }
        }

        private void PingHeartbeat()
        {
            float nextPingTime = _lastPingTime + HeartbeatInterval;
            bool isPingCooldownExpired = Time.time >= nextPingTime;

            if (isPingCooldownExpired)
            {
                _lastPingTime = Time.time;

                SignalingMessagePing message = new SignalingMessagePing();
                message.Type = SignalingMessageType.Ping;

                _signalingWebClient.Send(message.ToBytes());
            }
        }

        private void OnWebClientConnected()
        {
            Log.Debug($"[{nameof(HostAllocationService)}] Sending allocation request...");

            SendRequestAllocation();
        }

        private void SendRequestAllocation()
        {
            SignalingMessageRequestAllocation message = new SignalingMessageRequestAllocation();
            message.Type = SignalingMessageType.RequestAllocation;

            _signalingWebClient.Send(message.ToBytes());
        }

        private void OnWebClientMessageJoinCodeAllocated(SignalingMessageJoinCodeAllocated message)
        {
            Log.Debug($"[{nameof(HostAllocationService)}] JoinCode allocated: {message.JoinCode}");
            _allocatedJoinCode = message.JoinCode;
            _isJoinCodeAllocated = true;

            OnJoinCodeUpdated?.Invoke();
        }

        private void OnWebClientDisconnected()
        {

        }
    }
}

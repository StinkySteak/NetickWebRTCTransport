using JamesFrowen.SimpleWeb;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Netick.Transport.WebRTC
{
    public class SignalingServer
    {
        private SimpleWebServer _webServer;
        private AllocationService _allocationService;

        public void Start(ushort port)
        {
            TcpConfig tcpConfig = new TcpConfig(noDelay: false, sendTimeout: 5000, receiveTimeout: 20_000);
            _webServer = new SimpleWebServer(5_000, tcpConfig, ushort.MaxValue, 5_000, new SslConfig());

            _webServer.onConnect += OnRemoteClientConnected;
            _webServer.onDisconnect += OnRemoteClientDisconnected;
            _webServer.onData += OnDataReceived;
            _webServer.onError += OnError;

            _webServer.Start(port);

            Log.Info($"[{nameof(SignalingServer)}]: Signaling server is listening at: {port}");

            _allocationService = new AllocationService();
        }

        public void Stop()
        {
            _webServer.Stop();
        }

        public void PollUpdate()
        {
            _webServer.ProcessMessageQueue();
        }

        private void OnError(int connectionId, Exception exception)
        {
            Log.Error($"[{nameof(SignalingServer)}]: Error: {exception}");
        }

        private void OnDataReceived(int connectionId, ArraySegment<byte> bytes)
        {
            string json = Encoding.UTF8.GetString(bytes);

            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new SignalingMessageConverter() }
            };

            SignalingMessage message = JsonConvert.DeserializeObject<SignalingMessage>(json, settings);

            Log.Info($"[{nameof(SignalingServer)}]: Message received: {message.Type}");

            switch (message)
            {
                case SignalingMessageRequestAllocation alloc:
                    OnMessageRequestAllocation(connectionId, alloc);
                    break;
                case SignalingMessageAnswer answer:
                    OnMessageAnswer(connectionId, answer);
                    break;
                case SignalingMessageOffer offer:
                    OnMessageOffer(connectionId, offer);
                    break;
                case SignalingMessagePing ping:
                    OnMessagePing(connectionId, ping);
                    break;
                default:
                    // Handle unknown or unexpected messages
                    break;
            }
        }

        private void OnMessagePing(int connectionId, SignalingMessagePing message)
        {
            if (_allocationService.TryGetSession(connectionId, out Session session, out int index))
            {
                _allocationService.SetSessionHeartbeat(index);

                SignalingMessagePing messageReply = new();
                messageReply.Type = SignalingMessageType.Ping;

                _webServer.SendOne(connectionId, message.ToBytes());
                return;
            }

            Log.Info($"[{nameof(SignalingServer)}]: Session ping failed to be found from: {connectionId}");
        }


        private void OnMessageRequestAllocation(int fromConnectionId, SignalingMessageRequestAllocation message)
        {
            if (!_allocationService.TryAllocateSession(fromConnectionId, out Session allocatedSession))
            {
                return;
            }

            SignalingMessageJoinCodeAllocated messageReply = new SignalingMessageJoinCodeAllocated();
            messageReply.Type = SignalingMessageType.JoinCodeAllocated;
            messageReply.JoinCode = allocatedSession.JoinCode;

            string json = JsonConvert.SerializeObject(messageReply);
            byte[] bytes = Encoding.ASCII.GetBytes(json);

            Log.Info($"[{nameof(SignalingServer)}]: Session allocated: {allocatedSession.JoinCode}");

            _webServer.SendOne(fromConnectionId, bytes);
        }


        private void OnMessageAnswer(int fromConnectionId, SignalingMessageAnswer message)
        {
            Log.Info($"Forwarding answer to: {message.ToConnectionId}");

            _webServer.SendOne(message.ToConnectionId, message.ToBytes());
        }

        private void OnMessageOffer(int fromConnectionId, SignalingMessageOffer message)
        {
            if (!_allocationService.TryGetSession(message.ToJoinCode, out Session session, out int index))
            {
                Debug.LogWarning($"join code not found: {message.ToJoinCode}");
                return;
            }

            int toConnectionId = session.OwnerConnectionId;
            message.FromConnectionId = fromConnectionId;

            _webServer.SendOne(toConnectionId, message.ToBytes());
        }

        private void OnRemoteClientDisconnected(int connectionId)
        {
            if (_allocationService.TryRemoveSession(connectionId, out Session expectedSession))
            {
                Log.Info($"[{nameof(SignalingServer)}]: Removed session: {expectedSession.JoinCode}, host was disconnected");
            }
        }

        private void OnRemoteClientConnected(int connectionId)
        {
            Log.Info($"[{nameof(SignalingServer)}]: a client has connected!");
        }
    }
}

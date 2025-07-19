using JamesFrowen.SimpleWeb;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace StinkySteak.N2D
{
    public class SignalingServer
    {
        private SimpleWebServer _webServer;
        private const string JoinCodePool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const int JoinCodeLength = 5;

        public struct Session
        {
            public string JoinCode;
            public int OwnerConnectionId;
            public DateTime LastHeartbeatTime;
        }

        private List<Session> _sessions = new();

        public void Start(ushort port)
        {
            TcpConfig tcpConfig = new TcpConfig(noDelay: false, sendTimeout: 5000, receiveTimeout: 20_000);
            _webServer = new SimpleWebServer(5_000, tcpConfig, ushort.MaxValue, 5_000, new SslConfig());

            _webServer.onConnect += OnRemoteClientConnected;
            _webServer.onDisconnect += OnRemoteClientDisconnected;
            _webServer.onData += OnDataReceived;
            _webServer.onError += OnError;

            _webServer.Start(port);

            Log.Debug($"[{nameof(SignalingServer)}]: Signaling server is listening at: {port}");
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

            Log.Debug($"[{nameof(SignalingServer)}]: Received: {message.Type}");

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
            if (TryGetSession(connectionId, out Session session, out int index))
            {
                Session updatedSession = session;
                updatedSession.LastHeartbeatTime = DateTime.UtcNow;

                _sessions[index] = session;

                SignalingMessagePing messageReply = new();
                messageReply.Type = SignalingMessageType.Ping;

                _webServer.SendOne(connectionId, message.ToBytes());
                return;
            }

            Log.Debug($"[{nameof(SignalingServer)}]: Ping from remote is not found");
        }

        private bool TryGetSession(int ownerConnectionId, out Session expectedSession, out int index)
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                Session session = _sessions[i];

                if (session.OwnerConnectionId == ownerConnectionId)
                {
                    expectedSession = session;
                    index = i;
                    return true;
                }
            }

            index = -1;
            expectedSession = default;
            return false;
        }

        private bool TryGetSession(string joinCode, out Session expectedSession, out int index)
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                Session session = _sessions[i];

                if (session.JoinCode == joinCode)
                {
                    expectedSession = session;
                    index = i;
                    return true;
                }
            }

            index = -1;
            expectedSession = default;
            return false;
        }

        private void OnMessageRequestAllocation(int fromConnectionId, SignalingMessageRequestAllocation message)
        {
            if (!TryGenerateJoinCode(out string joinCode))
            {
                return;
            }

            SignalingMessageJoinCodeAllocated messageReply = new SignalingMessageJoinCodeAllocated();
            messageReply.Type = SignalingMessageType.JoinCodeAllocated;
            messageReply.JoinCode = joinCode;

            string json = JsonConvert.SerializeObject(messageReply);
            byte[] bytes = Encoding.ASCII.GetBytes(json);

            Session session = new Session()
            {
                JoinCode = joinCode,
                OwnerConnectionId = fromConnectionId,
                LastHeartbeatTime = DateTime.UtcNow,
            };

            _sessions.Add(session);

            Log.Debug($"[{nameof(SignalingServer)}]: Session allocated: {joinCode}");

            _webServer.SendOne(fromConnectionId, bytes);
        }

        private bool TryGenerateJoinCode(out string freeJoinCode)
        {
            char[] joinCode = new char[JoinCodeLength];

            for (int i = 0; i < JoinCodeLength; i++)
            {
                int randomIndexFromPool = UnityEngine.Random.Range(0, JoinCodePool.Length);

                char randomChar = JoinCodePool[randomIndexFromPool];

                joinCode[i] = randomChar;
            }

            string joinCodeStr = new(joinCode);

            for (int i = 0; i < _sessions.Count; i++)
            {
                var session = _sessions[i];

                bool isDuplicate = session.JoinCode == joinCodeStr;

                if (isDuplicate)
                {
                    freeJoinCode = string.Empty;
                    return false;
                }
            }

            freeJoinCode = joinCodeStr;
            return true;
        }

        private void OnMessageAnswer(int fromConnectionId, SignalingMessageAnswer message)
        {
            Log.Debug($"Forwarding answer to: {message.ToConnectionId}");

            _webServer.SendOne(message.ToConnectionId, message.ToBytes());
        }

        private void OnMessageOffer(int fromConnectionId, SignalingMessageOffer message)
        {
            if (!TryGetSession(message.ToJoinCode, out Session session, out int index))
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
            //if (TryRemoveSession(connectionId, out Session expectedSession))
            //{
            //    Debug.Log($"[{nameof(SignalingServer)}]: Removed session: {expectedSession.JoinCode}, host was disconnected");
            //}
        }

        private void OnRemoteClientConnected(int connectionId)
        {
            Log.Debug($"[{nameof(SignalingServer)}]: a client has connected!");
        }
    }
}

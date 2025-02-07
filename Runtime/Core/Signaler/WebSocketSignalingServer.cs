using System;
using System.Text;
using JamesFrowen.SimpleWeb;
using Newtonsoft.Json;
using UnityEngine;

namespace Netick.Transport.WebRTC
{
    public class WebSocketSignalingServer
    {
        private SimpleWebServer _server;

        public event Action<int> OnClientDisconnected;
        public event Action<int, string> OnClientOffered;

        public void Start(ushort listenPort)
        {
            TcpConfig tcpConfig = new(noDelay: true, sendTimeout: 5_000, receiveTimeout: 20_000);
            _server = new SimpleWebServer(5000, tcpConfig, ushort.MaxValue, 5_000, new SslConfig());

            _server.onConnect += OnConnect;
            _server.onData += OnData;
            _server.onDisconnect += OnDisconnect;
            _server.onError += OnError;

            _server.Start(listenPort);
            Debug.Log($"Signaling server has been started to listen on: {listenPort}");
        }

        public void PollUpdate()
        {
            _server.ProcessMessageQueue();
        }

        public void Stop()
        {
            _server.Stop();
        }

        private void OnError(int clientId, Exception exception)
        {
        }

        private void OnDisconnect(int clientId)
        {
            OnClientDisconnected?.Invoke(clientId);
        }

        public void SendAnswerToClient(int clientId, string answer)
        {
            SignalingMessage message = new();
            message.Content = answer;
            message.Type = SignalingMessageType.Answer;
            message.To = clientId;

            string json = JsonConvert.SerializeObject(message);
            byte[] data = Encoding.UTF8.GetBytes(json);

            _server.SendOne(clientId, new ArraySegment<byte>(data));
        }

        private void OnData(int clientId, ArraySegment<byte> data)
        {
            string json = Encoding.UTF8.GetString(data);

            SignalingMessage message = JsonConvert.DeserializeObject<SignalingMessage>(json);

            if (message.Type == SignalingMessageType.Offer)
            {
                string offer = message.Content;

                OnClientOffered?.Invoke(clientId, offer);
            }
        }

        private void OnConnect(int clientId)
        {
            Debug.Log($"Signaling server: client_{clientId} is connected");
        }
    }
}

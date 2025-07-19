using JamesFrowen.SimpleWeb;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace StinkySteak.N2D
{
    public class SignalingWebClient
    {
        private SimpleWebClient _webClient;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<Exception> OnError;
        public event Action<SignalingMessageRequestAllocation> OnMessageRequestAllocation;
        public event Action<SignalingMessageJoinCodeAllocated> OnMessageJoinCodeAllocated;
        public event Action<SignalingMessagePing> OnMessagePing;
        public event Action<SignalingMessageAnswer> OnMessageAnswer;
        public event Action<SignalingMessageOffer> OnMessageOffer;

        public void Connect(SignalingServerConnectConfig connectConfig)
        {
            TcpConfig tcpConfig = new TcpConfig(noDelay: false, sendTimeout: 5000, receiveTimeout: 20000);
            _webClient = SimpleWebClient.Create(ushort.MaxValue, 5000, tcpConfig);

            _webClient.onConnect += OnWebClientConnected;
            _webClient.onDisconnect += OnWebClientDisconnect;
            _webClient.onData += OnWebClientDataReceived;
            _webClient.onError += OnWebClientError;

            UriBuilder builder = new UriBuilder
            {
                Scheme = connectConfig.ConnectSecurely ? "wss" : "ws",
                Host = connectConfig.Address,
                Port = connectConfig.Port,
            };

            _webClient.Connect(builder.Uri);
        }

        public void PollUpdate()
        {
            _webClient?.ProcessMessageQueue();
        }

        public void Stop()
        {
            if (_webClient.ConnectionState == ClientState.Connected)
                _webClient.Disconnect();
        }

        private void OnWebClientConnected()
        {
            OnConnected?.Invoke();
        }
        private void OnWebClientDisconnect()
        {
            OnDisconnected?.Invoke();
        }

        public void Send(byte[] bytes)
        {
            _webClient.Send(bytes);
        }

        private void OnWebClientDataReceived(ArraySegment<byte> bytes)
        {
            string json = Encoding.UTF8.GetString(bytes);

            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new SignalingMessageConverter() }
            };

            SignalingMessage message = JsonConvert.DeserializeObject<SignalingMessage>(json, settings);
            Debug.Log($"[{nameof(SignalingWebClient)}]: Message received type: {message.Type}");

            switch (message)
            {
                case SignalingMessageRequestAllocation alloc:
                    OnMessageRequestAllocation?.Invoke(alloc);
                    break;
                case SignalingMessageAnswer answer:
                    OnMessageAnswer?.Invoke(answer);
                    break;
                case SignalingMessageOffer offer:
                    OnMessageOffer?.Invoke(offer);
                    break;
                case SignalingMessageJoinCodeAllocated joinCodeAllocated:
                    OnMessageJoinCodeAllocated?.Invoke(joinCodeAllocated);
                    break;
                default:
                    // Handle unknown or unexpected messages
                    break;
            }
        }

        private void OnWebClientError(Exception exception)
        {
            OnError?.Invoke(exception);
        }
    }
}

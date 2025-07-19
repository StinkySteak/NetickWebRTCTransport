using Newtonsoft.Json;
using System.Text;
using Unity.WebRTC;
using UnityEngine;

namespace StinkySteak.N2D
{
    [System.Serializable]
    public struct SignalingServerConnectConfig
    {
        public string Address;
        public int Port;
        public bool ConnectSecurely;
    }


    public struct SignalingServerListenConfig
    {
        public int Port;
    }

    public class SignalingMessage
    {
        public SignalingMessageType Type;

        public byte[] ToBytes()
        {
            string json = JsonConvert.SerializeObject(this);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            return bytes;
        }
    }

    public class SignalingMessageRequestAllocation : SignalingMessage
    {
    }

    public class SignalingMessageJoinCodeAllocated : SignalingMessage
    {
        public string JoinCode;
    }

    public class SignalingMessagePing : SignalingMessage
    {
    }

    public class SignalingMessageAnswer : SignalingMessage
    {
        public RTCSessionDescription Answer;
        public int ToConnectionId;
    }

    public class SignalingMessageOffer : SignalingMessage
    {
        public RTCSessionDescription Offer;
        public string ToJoinCode;
        public int FromConnectionId;
    }

    public enum SignalingMessageType
    {
        RequestAllocation,
        JoinCodeAllocated,
        Answer,
        Offer,
        Ping,
    }
}

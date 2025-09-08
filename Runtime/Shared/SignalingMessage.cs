using Newtonsoft.Json;
using StinkySteak.WebRealtimeCommunication;
using System.Text;

namespace Netick.Transport.WebRTC
{
    [System.Serializable]
    public struct SignalingServerConnectConfig
    {
        public string Address;
        public int Port;
        public bool ConnectSecurely;

        public bool ShutdownOnDisconnect;
    }

    public struct SignalingServerListenConfig
    {
        public int Port;
    }

    internal class SignalingMessage
    {
        public SignalingMessageType Type;

        public byte[] ToBytes()
        {
            string json = JsonConvert.SerializeObject(this);

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            return bytes;
        }
    }

    internal class SignalingMessageRequestAllocation : SignalingMessage
    {
    }
    internal class SignalingMessageJoinCodeAllocated : SignalingMessage
    {
        public string JoinCode;
    }
    internal class SignalingMessagePing : SignalingMessage
    {
    }

    internal class SignalingMessageAnswer : SignalingMessage
    {
        public WebRTCSessionDescription Answer;
        public int ToConnectionId;
    }

    internal class SignalingMessageOffer : SignalingMessage
    {
        public WebRTCSessionDescription Offer;
        public string ToJoinCode;
        public int FromConnectionId;
    }
    internal enum SignalingMessageType
    {
        RequestAllocation,
        JoinCodeAllocated,
        Answer,
        Offer,
        Ping,
    }
}

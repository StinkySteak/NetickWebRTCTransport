using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace StinkySteak.WebRealtimeCommunication
{
    public static class Browser
    {
#if UNITY_WEBGL
        [DllImport("__Internal")]
        public static extern int WebRTC_Unsafe_CreateRTCPeerConnection(string configJson);

        public static int WebRTC_CreateRTCPeerConnection(BrowserRTCConfiguration config)
        {
            StringEnumConverter settings = new StringEnumConverter()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            string json = JsonConvert.SerializeObject(config, settings);

            return WebRTC_Unsafe_CreateRTCPeerConnection(json);
        }

        [DllImport("__Internal")]
        public static extern bool WebRTC_GetOpCreateOfferIsDone(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_GetOpCreateAnswerIsDone(int index);

        [DllImport("__Internal")]
        public static extern void WebRTC_DisposeOpCreateOffer(int index);

        [DllImport("__Internal")]
        public static extern void WebRTC_DisposeOpCreateAnswer(int index);

        [DllImport("__Internal")]
        public static extern void WebRTC_CreateOffer(int index);

        [DllImport("__Internal")]
        public static extern void WebRTC_CreateAnswer(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpCreateAnswer(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpSetRemoteDescription(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_IsOpSetRemoteDescriptionDone(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpCreateOffer(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_CloseConnection(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpSetLocalDescription(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_IsOpSetLocalDescriptionDone(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_DisposeOpSetLocalDescription(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_DisposeOpSetRemoteDescription(int index);

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetConnectionState(int index);

        public static BrowserRTCIceGatheringState WebRTC_GetGatheringState(int index)
        {
            return (BrowserRTCIceGatheringState)WebRTC_Unsafe_GetGatheringState(index);
        }

        [DllImport("__Internal")]
        public static extern int WebRTC_Unsafe_GetGatheringState(int index);

        public static string WebRTC_GetConnectionState(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetConnectionState(index);

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string offer = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            return offer;
        }

        public static string WebRTC_GetOfferJson(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetOffer(index);

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string offer = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            return offer;
        }

        public static string WebRTC_GetAnswerJson(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetAnswer(index);

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string offer = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            return offer;
        }

        public static WebRTCSessionDescription WebRTC_GetOffer(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetOffer(index);

            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            string json = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            WebRTCSessionDescription sdp = JsonConvert.DeserializeObject<WebRTCSessionDescription>(json);

            return sdp;
        }

        public static WebRTCSessionDescription WebRTC_GetAnswer(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetAnswer(index);

            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            string json = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            WebRTCSessionDescription sdp = JsonConvert.DeserializeObject<WebRTCSessionDescription>(json);

            return sdp;
        }

        public static WebRTCSessionDescription WebRTC_GetLocalDescription(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetLocalDescription(index);

            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            string sdpJson = Marshal.PtrToStringAuto(ptr);
            Marshal.FreeHGlobal(ptr);

            WebRTCSessionDescription sdp = JsonConvert.DeserializeObject<WebRTCSessionDescription>(sdpJson);

            return sdp;
        }

        public static string WebRTC_GetRemoteDescriptionJson(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetRemoteDescription(index);

            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            string sdpJson = Marshal.PtrToStringAuto(ptr);

            return sdpJson;
        }

        public static WebRTCSessionDescription WebRTC_GetRemoteDescription(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetRemoteDescription(index);

            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            string sdpJson = Marshal.PtrToStringAuto(ptr);
            Marshal.FreeHGlobal(ptr);

            WebRTCSessionDescription sdp = JsonConvert.DeserializeObject<WebRTCSessionDescription>(sdpJson);

            return sdp;
        }

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetOffer(int index);

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetAnswer(int index);

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() } }
        };

        public static void WebRTC_SetLocalDescription(int index, WebRTCSessionDescription sessionDescription)
        {
            string json = JsonConvert.SerializeObject(sessionDescription, _jsonSettings);
            WebRTC_Unsafe_SetLocalDescription(index, json);
        }

        public static void WebRTC_SetRemoteDescription(int index, WebRTCSessionDescription sessionDescription)
        {
            string json = JsonConvert.SerializeObject(sessionDescription, _jsonSettings);
            WebRTC_Unsafe_SetRemoteDescription(index, json);
        }

        [DllImport("__Internal")]
        public static extern void WebRTC_Unsafe_SetLocalDescription(int index, string sdpJson);

        [DllImport("__Internal")]
        public static extern void WebRTC_Unsafe_SetRemoteDescription(int index, string sdpJson);

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetLocalDescription(int index);

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetRemoteDescription(int index);

        public static void WebRTC_CreateDataChannel(int index, BrowserRTCDataChannelInit dataChannelConfig)
        {
            string json = JsonConvert.SerializeObject(dataChannelConfig);

            WebRTC_Unsafe_CreateDataChannel(index, json);
        }

        [DllImport("__Internal")]
        public static extern void WebRTC_Unsafe_CreateDataChannel(int index, string configJson);

        public static void WebRTC_CreateDataChannelReliable(int index)
        {
            WebRTC_Unsafe_CreateDataChannelReliable(index);
        }

        [DllImport("__Internal")]
        public static extern void WebRTC_Unsafe_CreateDataChannelReliable(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_IsConnectionOpen(int index);

        [DllImport("__Internal")]
        public static extern void WebRTC_DataChannelSend(int index, IntPtr ptr, int length);

        [DllImport("__Internal")]
        public static extern void WebRTC_DataChannelReliableSend(int index, IntPtr ptr, int length);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnMessage(int index, OnMessageCallback callback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnIceConnectionStateChange(int index, OnIceConnectionStateChange callback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnDataChannelCreated(int index, OnDataChannelCreated calback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnIceCandidate(int index, OnIceCandidate calback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnDataChannelOpen(int index, OnDataChannelOpen calback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnDataChannelReliableOpen(int index, OnDataChannelOpen calback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnIceCandidateGatheringState(int index, OnIceCandidateGatheringState calback);

        [DllImport("__Internal")]
        public static extern bool WebRTC_GetIsPeerConnectionCreated(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_Reset(int index);
#else
        public static void WebRTC_Unsafe_CreateRTCPeerConnection(string configJson) { }

        public static void WebRTC_CreateRTCPeerConnection(BrowserRTCConfiguration config)
        {
            string json = JsonConvert.SerializeObject(config);
            WebRTC_Unsafe_CreateRTCPeerConnection(json);
        }

        public static bool WebRTC_GetOpCreateOfferIsDone() { return false; }
        public static bool WebRTC_GetOpCreateAnswerIsDone() { return false; }
        public static void WebRTC_DisposeOpCreateOffer() { }
        public static void WebRTC_DisposeOpCreateAnswer() { }
        public static void WebRTC_CreateOffer() { }
        public static void WebRTC_CreateAnswer() { }
        public static bool WebRTC_HasOpCreateAnswer() { return false; }
        public static bool WebRTC_HasOpSetRemoteDescription() { return false; }
        public static bool WebRTC_IsOpSetRemoteDescriptionDone() { return false; }
        public static bool WebRTC_HasOpCreateOffer() { return false; }
        public static bool WebRTC_CloseConnection() { return false; }
        public static bool WebRTC_HasOpSetLocalDescription() { return false; }
        public static bool WebRTC_IsOpSetLocalDescriptionDone() { return false; }
        public static bool WebRTC_DisposeOpSetLocalDescription() { return false; }
        public static bool WebRTC_DisposeOpSetRemoteDescription() { return false; }
        public static IntPtr WebRTC_Unsafe_GetConnectionState() { return IntPtr.Zero; }

        public static BrowserRTCIceGatheringState WebRTC_GetGatheringState()
        {
            return (BrowserRTCIceGatheringState)WebRTC_Unsafe_GetGatheringState();
        }

        public static int WebRTC_Unsafe_GetGatheringState() { return 0; }

        public static string WebRTC_GetConnectionState()
        {
            return string.Empty;
        }

        public static string WebRTC_GetOffer()
        {
            return string.Empty;
        }

        public static string WebRTC_GetAnswer()
        {
            return string.Empty;
        }

        public static string WebRTC_GetLocalDescription()
        {
            return string.Empty;
        }

        public static string WebRTC_GetRemoteDescription()
        {
            return string.Empty;
        }

        public static IntPtr WebRTC_Unsafe_GetOffer() { return IntPtr.Zero; }
        public static IntPtr WebRTC_Unsafe_GetAnswer() { return IntPtr.Zero; }
        public static void WebRTC_SetLocalDescription(string sdp) { }
        public static void WebRTC_SetRemoteDescription(string sdp) { }
        public static IntPtr WebRTC_Unsafe_GetLocalDescription() { return IntPtr.Zero; }
        public static IntPtr WebRTC_Unsafe_GetRemoteDescription() { return IntPtr.Zero; }

        public static void WebRTC_CreateDataChannel(BrowserRTCDataChannelInit dataChannelConfig)
        {
            string json = JsonConvert.SerializeObject(dataChannelConfig);
            WebRTC_Unsafe_CreateDataChannel(json);
        }

        public static void WebRTC_Unsafe_CreateDataChannel(string configJson) { }
        public static void WebRTC_Unsafe_CreateDataChannelReliable() { }
        public static bool WebRTC_IsConnectionOpen() { return false; }
        public static void WebRTC_DataChannelSend(IntPtr ptr, int length) { }
        public static void WebRTC_SetCallbackOnMessage(OnMessageCallback callback) { }
        public static void WebRTC_SetCallbackOnIceConnectionStateChange(OnIceConnectionStateChange callback) { }
        public static void WebRTC_SetCallbackOnDataChannelCreated(OnDataChannelCreated callback) { }
        public static void WebRTC_SetCallbackOnIceCandidate(OnIceCandidate callback) { }
        public static void WebRTC_SetCallbackOnDataChannelOpen(OnDataChannelOpen callback) { }
        public static void WebRTC_SetCallbackOnDataChannelReliableOpen(OnDataChannelOpen callback) { }
        public static void WebRTC_SetCallbackOnDataChanneReliablelOpen(OnDataChannelReliableOpen callback) { }
        public static void WebRTC_SetCallbackOnIceCandidateGatheringState(OnIceCandidateGatheringState callback) { }
        public static bool WebRTC_GetIsPeerConnectionCreated() { return false; }
        public static bool WebRTC_Reset() { return false; }

#endif
    }

    public delegate void OnMessageCallback(int index, IntPtr ptr, int length);
    public delegate void OnIceConnectionStateChange(int index);
    public delegate void OnDataChannelCreated(int index);
    public delegate void OnIceCandidate(int index);
    public delegate void OnDataChannelOpen(int index);
    public delegate void OnDataChannelReliableOpen(int index);
    public delegate void OnIceCandidateGatheringState(int index, int state);

    public enum BrowserRTCIceGatheringState : int
    {
        New = 0,
        Gathering = 1,
        Complete = 2
    }

    public struct BrowserRTCConfiguration
    {
        public BrowserRTCIceServer[] iceServers;
    }

    public struct BrowserRTCIceServer
    {
        public string[] urls;
        public BrowserRTCIceCredentialType credentialType;
        public string username;
        public string credential;
    }

    public struct BrowserRTCSessionDescription
    {
        public string type;
        public string sdp;
    }

    public struct WebRTCSessionDescription
    {
        public RTCSdpType type;
        public string sdp;
    }

    public enum RTCSdpType
    {
        Offer,
        Pranswer,
        Answer,
        Rollback
    }

    public enum BrowserRTCIceCredentialType
    {
        Password,
        OAuth
    }

    public struct BrowserRTCDataChannelInit
    {
        public bool ordered;
        public int maxRetransmits;
    }
}
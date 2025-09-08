using UnityEngine;

namespace Netick.Transport.WebRTC
{
    [System.Serializable]
    public struct UserRTCConfig
    {
        public IceServer[] IceServers;
        public float RTCTimeoutDuration;
        public IceCandidateGatheringConfig IceCandidateGatheringConfig;
    }

    [System.Serializable]
    public struct IceCandidateGatheringConfig
    {
        [Tooltip("WebRTC can takes some time to gather all of the candidates. Leave empty if you want to gather candidates in a fixed time instead (Recommended: False)")]
        public bool WaitGatheringToComplete;

        [Tooltip("This parameter is only valid if the WaitGatheringToComplete is disabled")]
        public float GatherDuration;

        public bool ManualGatheringStop => !WaitGatheringToComplete;
    }
}

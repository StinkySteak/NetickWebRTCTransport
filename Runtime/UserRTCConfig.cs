using System;
using Unity.WebRTC;
using UnityEngine;

namespace StinkySteak.N2D
{
    [System.Serializable]
    public struct UserRTCConfig
    {
        public RTCIceServer[] IceServers;
        public float TimeoutDuration;
        public IceCandidateGatheringConfig IceCandidateGatheringConfig;
    }

    [System.Serializable]
    public struct IceCandidateGatheringConfig
    {
        [Tooltip("WebRTC can takes some time to gather all of the candidates. Leave empty if you want to don't want to wait until finished")]
        public bool WaitGatheringToComplete;

        [Tooltip("This parameter is only valid if the WaitGatheringToComplete is disabled")]
        public float GatherDuration;
    }
}

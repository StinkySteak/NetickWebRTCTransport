## Overview

Utilizing WebRTC as a transport for Netick. Allowing Developers to STUN and TURN (Relay)

If you are new to WebRTC, check out the resources section down below

## Variants
- [WebRTC Embdedded Transport](https://github.com/StinkySteak/NetickWebRTCEmbeddedTransport) is a variant of webRTC transport that has signaling server embedded, recommended for dedicated server games. 

### Target Platform

| Target Platform        | Description                                  | Status       |
|----------------|----------------------------------------------|--------------|
| Native Support | Based on the Unity WebRTC supported platform | Experimental |
| WebGL Support  | WebGL acting as a client                     | Not yet supported |

## Installation

### Prerequisites

Unity Editor version 2021 or later.

Install Netick 2 before installing this package.
https://github.com/NetickNetworking/NetickForUnity

### Dependencies
1. [UnityWebRTC 3.0.0-pre-8](https://github.com/Unity-Technologies/com.unity.webrtc) (Core functionality)
1. [SimpleWebTransport](https://github.com/James-Frowen/SimpleWebTransport) (As Signaling Server) (UPM: `https://github.com/James-Frowen/SimpleWebTransport.git?path=source`)
1. [FlexTimer](https://github.com/StinkySteak/UnityFlexTimer)
1. [Newtonsoft Json Unity](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html)

### Steps

- Open the Unity Package Manager by navigating to Window > Package Manager along the top bar.
- Click the plus icon.
- Select Add package from git URL
- Enter https://github.com/StinkySteak/NetickWebRTCTransport.git
- You can then create an instance by double clicking in the Assets folder and going to `Create > Netick > Transport > NetickWebRTCTransport`

## Accessing Join code
You can attach this script to the NetworkSandbox and let view component access the join code to there.
```cs
using Netick.Transport;
using Netick.Unity;

public class JoinCodeSandbox : NetickBehaviour
{
    public string JoinCode;
    private WebRTCTransport _transport;

    public override void NetworkStart()
    {
        if (Object.IsServer)
        {
            _transport = Sandbox.Transport as WebRTCTransport;
            _transport.HostAllocationService.OnJoinCodeUpdated += UpdateJoinCode;
            _transport.HostAllocationService.OnTimeoutFromSignalingServer += OnDisconnectedFromSignalingServer;
            _transport.HostAllocationService.OnDisconnectedFromSignalingServer += OnDisconnectedFromSignalingServer;

            UpdateJoinCode();
        }
    }

    private void OnDisconnectedFromSignalingServer()
    {
        UnityEngine.Debug.LogError("Failed to register to signaling server");
        // Show UI Error in-game
    }

    private void UpdateJoinCode()
    {
        JoinCode = _transport.HostAllocationService.AllocatedJoinCode;
    }
}


```

### Resources to learn WebRTC
- [Simple WebRTC Introduction](https://www.youtube.com/watch?v=8I2axE6j204)
- [Unity Sample](https://docs.unity3d.com/Packages/com.unity.webrtc@3.0/manual/sample.html)

## Overview

Utilizing WebRTC as a transport for Netick. Allowing Developers to Utilize DTLS (Secure UDP) for WebGL and Native platform!

If you are new to WebRTC, check out the resources section down below

## Variants
- [WebRTC Embdedded Transport](https://github.com/StinkySteak/NetickWebRTCEmbeddedTransport) is a variant of webRTC transport that has signaling server embedded, recommended for dedicated server games. 

### Features

| Feature        | Description                                  | Status       |
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

### How to Use?

| API                    | Description                                                                                            |
|------------------------|--------------------------------------------------------------------------------------------------------|
| Timeout Duration | Define how long timeout will be called upon failed to connect |
| ICE Servers            | URLs of your STUN/TURN servers                                                                         |

### HTTPS/WSS Support
Enable `ConnectSecurely` on the transport then, do one of these:

1. [Through Reverse Proxy (Recommended)](https://caddyserver.com/docs/quick-starts/reverse-proxy)
2. [Through SSL Certificate](https://github.com/StinkySteak/SimpleWebTransport/blob/master/HowToCreateSSLCert.md)

### Resources to learn WebRTC
- [Simple WebRTC Introduction](https://www.youtube.com/watch?v=8I2axE6j204)
- [Unity Sample](https://docs.unity3d.com/Packages/com.unity.webrtc@3.0/manual/sample.html)

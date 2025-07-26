using System;

namespace Netick.Transport.WebRTC
{
    public struct Session
    {
        public string JoinCode;
        public int OwnerConnectionId;
        public DateTime LastHeartbeatTime;
    }
}

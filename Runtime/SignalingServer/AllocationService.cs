using System;
using System.Collections.Generic;

namespace Netick.Transport.WebRTC
{
    public class AllocationService
    {
        private const string JoinCodePool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const int JoinCodeLength = 5;

        private List<Session> _sessions = new();

        public bool TryGetSession(int ownerConnectionId, out Session expectedSession, out int index)
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                Session session = _sessions[i];

                if (session.OwnerConnectionId == ownerConnectionId)
                {
                    expectedSession = session;
                    index = i;
                    return true;
                }
            }

            index = -1;
            expectedSession = default;
            return false;
        }

        public bool TryGetSession(string joinCode, out Session expectedSession, out int index)
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                Session session = _sessions[i];

                if (session.JoinCode == joinCode)
                {
                    expectedSession = session;
                    index = i;
                    return true;
                }
            }

            index = -1;
            expectedSession = default;
            return false;
        }

        public bool TryGenerateJoinCode(out string freeJoinCode)
        {
            char[] joinCode = new char[JoinCodeLength];

            for (int i = 0; i < JoinCodeLength; i++)
            {
                int randomIndexFromPool = UnityEngine.Random.Range(0, JoinCodePool.Length);

                char randomChar = JoinCodePool[randomIndexFromPool];

                joinCode[i] = randomChar;
            }

            string joinCodeStr = new(joinCode);

            for (int i = 0; i < _sessions.Count; i++)
            {
                var session = _sessions[i];

                bool isDuplicate = session.JoinCode == joinCodeStr;

                if (isDuplicate)
                {
                    freeJoinCode = string.Empty;
                    return false;
                }
            }

            freeJoinCode = joinCodeStr;
            return true;
        }

        public bool TryGenerateJoinCodeFast(out string freeJoinCode)
        {
            char[] joinCode = new char[JoinCodeLength];

            for (int i = 0; i < JoinCodeLength; i++)
            {
                int randomIndexFromPool = UnityEngine.Random.Range(0, JoinCodePool.Length);

                char randomChar = JoinCodePool[randomIndexFromPool];

                joinCode[i] = randomChar;
            }

            string joinCodeStr = new(joinCode);

            for (int i = 0; i < _sessions.Count; i++)
            {
                var session = _sessions[i];

                bool isDuplicate = session.JoinCode == joinCodeStr;

                if (isDuplicate)
                {
                    freeJoinCode = string.Empty;
                    return false;
                }
            }

            freeJoinCode = joinCodeStr;
            return true;
        }

        public void SetSessionHeartbeat(int index)
        {
            Session session = _sessions[index];
            session.LastHeartbeatTime = DateTime.UtcNow;

            _sessions[index] = session;
        }

        public bool TryAllocateSession(int ownerConnectionId, out Session allocatedSession)
        {
            if (!TryGenerateJoinCode(out string joinCode))
            {
                allocatedSession = default;
                return false;
            }

            Session session = new Session()
            {
                JoinCode = joinCode,
                OwnerConnectionId = ownerConnectionId,
                LastHeartbeatTime = DateTime.UtcNow,
            };

            allocatedSession = session;

            _sessions.Add(session);
            return true;
        }

        public bool TryRemoveSession(int ownerConnectionId, out Session expectedSession)
        {
            bool isFound = false;
            int index = 0;
            expectedSession = default;

            for (int i = 0; i < _sessions.Count; i++)
            {
                Session session = _sessions[i];

                if (session.OwnerConnectionId == ownerConnectionId)
                {
                    isFound = true;
                    index = i;
                    expectedSession = session;
                    break;
                }
            }

            if (isFound)
            {
                _sessions.RemoveAt(index);
                return true;
            }

            return false;
        }
    }
}

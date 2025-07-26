namespace Netick.Transport.WebRTC
{
    internal static class Log
    {
        private static bool _enabled = true;

        public static void Enable(bool enable)
        {
            _enabled = enable;
        }

        public static void Info(object message)
        {
            if (_enabled)
                UnityEngine.Debug.Log(message);
        }

        public static void Warning(object message)
        {
            if (_enabled)
                UnityEngine.Debug.LogWarning(message);
        }

        public static void Error(object message)
        {
            if (_enabled)
                UnityEngine.Debug.LogError(message);
        }
    }
}

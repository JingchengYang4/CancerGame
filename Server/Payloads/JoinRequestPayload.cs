using System;
namespace SignalRServer.Payloads
{
    [Serializable]
    public class JoinRequestPayload
    {
        public string connectionID { get; set; }
        public string roomID { get; set; }
    }
}


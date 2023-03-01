using System;
namespace SignalRServer.Payloads
{
    [Serializable]
    public class TurnPayload
    {
        public string connectionID { get; set; }
        public int side { get; set; }
        public int counter { get; set; }
    }
}


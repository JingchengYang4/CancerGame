using System;
using System.Collections.Generic;

namespace SignalRServer.Payloads
{
    [Serializable]
    public class TeamStatus
	{
        public List<string> patientPlayers { get; set; } = new List<string>();
        public List<string> cancerPlayers { get; set; } = new List<string>();
    }
}


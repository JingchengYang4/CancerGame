using System;
using System.Collections.Generic;

[Serializable]
public class JoinRequestPayload
{
    public string connectionID { get; set; }
    public string roomID { get; set; }
}

[Serializable]
public class TeamStatus
{
    public List<string> patientPlayers { get; set; } = new List<string>();
    public List<string> cancerPlayers { get; set; } = new List<string>();
}

[Serializable]
public class TurnPayload
{
    public string connectionID { get; set; }
    public int side { get; set; }
    public int counter { get; set; }
}
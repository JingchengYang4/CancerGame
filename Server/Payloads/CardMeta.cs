using System;
namespace SignalRServer.Payloads
{
    [Serializable]
    public class CardMeta
    {
        public string cardName { get; set; }

        public float health { get; set; } = 0;
        public float emotion { get; set; } = 0;
        public float economy { get; set; } = 0;
        public float cancer { get; set; } = 0;

        public int index { get; set; }
    }
}


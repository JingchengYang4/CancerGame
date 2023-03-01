using System;
using Newtonsoft.Json;
using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Cancer/CardMeta", order = 20)]
[Serializable]
public class CardMeta : ScriptableObject
{
    public string cardName { get; set; }
    
    [JsonIgnore]
    public Sprite sprite;

    [field: SerializeField]
    public float health { get; set; } = 0;
    
    [field: SerializeField]
    public float emotion { get; set; } = 0;
    
    [field: SerializeField]
    public float economy { get; set; } = 0;
    
    [field: SerializeField]
    public float cancer { get; set; } = 0;
    
    public int index { get; set; }
}


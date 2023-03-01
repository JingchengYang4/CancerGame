using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSlot : MonoBehaviour
{
    public Transform cardStartingPosition;

    public List<Card> cards = new List<Card>();

    public int mouseDown = 0;

    public void AddCard(Card card)
    {
        card.state = CardState.Slot;
        card.slotPos = cardStartingPosition.position + new Vector3(1, 0, -0.2f) * cards.Count;
        card.index = cards.Count;
        cards.Add(card);
    }

    public void RemoveCard(Card card)
    {
        cards.RemoveAt(card.index);
        for(int i = 0; i < cards.Count; i++)
        {
            cards[i].slotPos = cardStartingPosition.position + new Vector3(1, 0, -0.2f) * i;
            cards[i].index = i;
        }
    }
}
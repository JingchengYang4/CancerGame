using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public Vector3 targetPos;
    public Vector3 slotPos;

    bool onMouseOver = false;

    public CardState state = CardState.None;

    public CardMeta meta;

    public SpriteRenderer renderer;
    public CardSlot slot;

    public int index = 0;

    private void Start()
    {
        slot = FindObjectOfType<CardSlot>();
        targetPos = transform.position;
    }

    private void Update()
    {

        targetPos = slotPos;
        var scale = Vector3.one * 0.1f;

        Color target = Color.white;

        if(state == CardState.Slot)
        {
            if (onMouseOver)
            {
                targetPos += new Vector3(0, 0.4f, 0);
                scale *= 1.3f;
                onMouseOver = false;
            }
            else if(slot.mouseDown > 0)
            {
                target = new Color(1, 1, 1, 0.2f);
            }
        }

        if(state != CardState.Decay)
        {
            renderer.color = Color.Lerp(renderer.color, target, Time.deltaTime * 5);
        }
        else if(state == CardState.Decay)
        {
            if(Vector3.Distance(transform.position, targetPos) <= 0.001f)
            {
                renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, renderer.color.a - Time.deltaTime * 2f);
                if(renderer.color.a <= 0)
                {
                    Destroy(gameObject);
                }
            }
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, scale, Time.deltaTime * 10);
    }

    private void OnMouseOver()
    {
        onMouseOver = true;
    }

    int entered = 0;

    private void OnMouseEnter()
    {
        if (state == CardState.Slot)
        {
            slot.mouseDown++;
            entered++;
        }
    }

    private void OnMouseDown()
    {
        if(state == CardState.Slot && !FindObjectOfType<GameManager>().inProgress)
        {
            if (entered > 0) slot.mouseDown--;
            state = CardState.Using;
            slotPos = Vector3.zero;
            FindObjectOfType<CardSlot>().RemoveCard(this);
            FindObjectOfType<GameManager>().UseCard(this);
            state = CardState.Decay;
        }
    }

    private void OnMouseExit()
    {
        if (state == CardState.Slot)
        {
            slot.mouseDown--;
            entered--;
        }
    }
}

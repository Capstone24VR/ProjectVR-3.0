using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Card : MonoBehaviour
{
    public enum Suit { None, Heart, Diamond, Club, Spade }
    public enum Value { Red, Green, Blue,
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    }



    public Suit suit;
    public Value val;

    [SerializeField] protected Vector3 _position = Vector3.zero;
    [SerializeField] protected Vector3 _localScale = Vector3.zero;

    public void Awake()
    {
        _localScale = transform.localScale;
    }


    public void SetPosition(Vector3 position)
    {
        _position = position;
    }


    public void ResetPosition()
    {
        transform.localPosition = _position;
        transform.localRotation = Quaternion.identity;
    }

    public void HoverSelect()
    {
        transform.localScale = _localScale * 1.25f;
    }

    public void HoverDeSelect()
    {
        transform.localScale = _localScale;
    }

}

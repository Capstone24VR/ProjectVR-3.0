using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Card : MonoBehaviour
{


    public enum Suit { Heart, Diamond, Club, Spades }

    public bool inHand = false;
    public enum Value
    {
        A = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        J = 11,
        Q = 12,
        K = 13
    }



    public Suit suit;
    public Value value;

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
    // Method to set the card owner ID

    // Method to set the card as "in hand"
    public void SetInHand(bool isInHand)
    {
        inHand = isInHand;
        if (inHand)
        {
            Debug.Log($"Card {suit} {value} is now in hand.");

        }
        else
        {
            Debug.Log($"Card {suit} {value} is no longer in hand.");
        }
    }
}
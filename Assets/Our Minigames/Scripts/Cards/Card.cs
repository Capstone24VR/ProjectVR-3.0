using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using XRMultiplayer.MiniGames;

public class Card : NetworkBehaviour
{
    public enum Suit { Heart, Diamond, Club, Spades }
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

    public bool inHand = false;
    public bool played = false;

    [SerializeField] protected Vector3 _position = Vector3.zero;
    [SerializeField] protected Vector3 _localScale = Vector3.one;

    private XRGrabInteractable _xrInteract;
    private NetworkedCards _cardManager;

    public void Awake()
    {
        _localScale = transform.localScale;
        _xrInteract = GetComponent<XRGrabInteractable>();

        _cardManager = FindAnyObjectByType<NetworkedCards>();
    }

    public void SetPosition(Vector3 position)
    {
        _position = position;
    }


    public void ResetPosition()
    {
        GetComponent<Rigidbody>().isKinematic = true;
        transform.localPosition = _position;
        transform.localRotation = Quaternion.identity;
    }

    // Hover select effect
    public void HoverSelect()
    {
        ScaleCard(_localScale * 1.25f);
    }

    // Hover deselect effect
    public void HoverDeSelect()
    {
        ScaleCard(_localScale);
    }

    private void ScaleCard(Vector3 newScale)
    {
        // Update local scale
        transform.localScale = newScale;

        // If the server is executing this, synchronize the scale to all clients
        if (IsServer)
        {
            RpcScaleCard(newScale);
        }
    }


    private void RpcScaleCard(Vector3 scale)
    {
        // Apply the scale change on all clients
        transform.localScale = scale;
    }

    public void SetInHand(bool isInHand)
    {
        inHand = isInHand;
        if (inHand)
        {
            // Debug.Log($"Card {suit} {value} is now in hand.");
        }
        else
        {
            // Debug.Log($"Card {suit} {value} is no longer in hand.");
        }
    }

    public string GetCardId()
    {
        return suit.ToString() + value.ToString();
    }

    // Use XR Interaction Toolkit's hover callbacks to trigger hover effects
    protected virtual void OnHoverEntered(HoverEnterEventArgs args)
    {
        HoverSelect(); // Trigger hover select effect
    }

    protected virtual void OnHoverExited(HoverExitEventArgs args)
    {
        HoverDeSelect(); // Trigger hover deselect effect
    }

    private void OnEnable()
    {
        if (_xrInteract != null)
        {
            _xrInteract.hoverEntered.AddListener(OnHoverEntered);
            _xrInteract.hoverExited.AddListener(OnHoverExited);
        }
    }

    private void OnDisable()
    {
        if (_xrInteract != null)
        {
            _xrInteract.hoverEntered.RemoveListener(OnHoverEntered);
            _xrInteract.hoverExited.RemoveListener(OnHoverExited);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using XRMultiplayer;
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
    public bool canBePlayed = false;

    [SerializeField] protected Vector3 _position = Vector3.zero;
    [SerializeField] protected Vector3 _localScale = Vector3.one;
    
    private XRGrabInteractable _xrInteract;
    private NetworkedCards _cardManager;
    private CardOwnerManager _ownerManger;

    private AudioSource _cardSFX;

    [Header("Sound Clips")]
    [SerializeField] private AudioClip _cardPickup;
    [SerializeField] private AudioClip _cardRelease;

    public void Awake()
    {
        _localScale = transform.localScale;
        _xrInteract = GetComponent<XRGrabInteractable>();

        _cardManager = FindAnyObjectByType<NetworkedCards>();
        _cardSFX = GetComponent<AudioSource>();
        _ownerManger = GetComponent<CardOwnerManager>();
    }

    public void SetPosition(Vector3 position)
    {
        if (IsSpawned) _position = position;
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

        // Call ServerRpc to inform the server of this hover event
        HoverSelectServerRpc();
    }

    // Hover deselect effect
    public void HoverDeSelect()
    {
        ScaleCard(_localScale);

        // Call ServerRpc to inform the server of this deselect event
        HoverDeSelectServerRpc();
    }

    private void ScaleCard(Vector3 newScale)
    {
        // Update local scale
        transform.localScale = newScale;
    }

    public void PlaySFX(int clip, ulong clientId)
    {
        PlaySFXServerRpc(clip, clientId);
    }

    public void PlaySFXAll(int clip)
    {
        PlaySFXAllServerRpc(clip);
    }

    // ServerRpc to inform the server about the hover select event
    [ServerRpc(RequireOwnership = false)]
    private void HoverSelectServerRpc()
    {
        // Server will inform all clients to apply the hover select effect
        HoverSelectClientRpc();
    }

    // ClientRpc to apply hover select on all clients
    [ClientRpc]
    private void HoverSelectClientRpc()
    {
        ScaleCard(_localScale * 1.25f); // Apply hover select effect on all clients
    }

    // ServerRpc to inform the server about the hover deselect event
    [ServerRpc(RequireOwnership = false)]
    private void HoverDeSelectServerRpc()
    {
        // Server will inform all clients to apply the hover deselect effect
        HoverDeSelectClientRpc();
    }

    // ClientRpc to apply hover deselect on all clients
    [ClientRpc]
    private void HoverDeSelectClientRpc()
    {
        ScaleCard(_localScale); // Apply hover deselect effect on all clients
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaySFXServerRpc(int clip, ulong clientId)
    {
        PlaySFXClientRpc(clip, clientId);
    }


    [ClientRpc]
    private void PlaySFXClientRpc(int clip, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            PlayAudioClip(clip);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaySFXAllServerRpc(int clip)
    {
        PlaySFXAllClientRpc(clip);
    }


    [ClientRpc]
    private void PlaySFXAllClientRpc(int clip)
    {
        PlayAudioClip(clip);
    }

    private void PlayAudioClip(int clip) {
        switch (clip)
        {
            case 0:
                _cardSFX.clip = _cardPickup;
                break;
            case 1:
                _cardSFX.clip = _cardRelease;
                break;
        }

        _cardSFX.Play();
    }

    public void SetCardInteractive(bool value)
    {
        _xrInteract.trackPosition = value;
        _xrInteract.trackRotation = value;
    }

    public void SetInHand(bool isInHand)
    {
        inHand = isInHand;
    }
    private bool HeldByOwner()
    {
        return _ownerManger.cardOwnerId == NetworkManager.Singleton.LocalClientId;
    }

    // Use XR Interaction Toolkit's hover callbacks to trigger hover effects
    protected virtual void OnHoverEntered(HoverEnterEventArgs args)
    {
        if(IsSpawned)
            HoverSelect(); // Trigger hover select effect
    }

    protected virtual void OnHoverExited(HoverExitEventArgs args)
    {
        if(IsSpawned)
            HoverDeSelect(); // Trigger hover deselect effect
    }

    protected virtual void OnSelectEntered(SelectEnterEventArgs args)
    {
        if(IsSpawned)
            HoverDeSelect();
        if(HeldByOwner())
            PlaySFXServerRpc(0, _ownerManger.cardOwnerId); // 0 indicates pickup SFX
    }

    protected virtual void OnSelectExited(SelectExitEventArgs args)
    {
        if (IsSpawned)
        {
            if (inHand && HeldByOwner())
                PlaySFXServerRpc(1, _ownerManger.cardOwnerId);
            ResetPosition();
            HoverDeSelect();
            _cardManager.RequestDrawCard(gameObject);
        }
    }

    private void OnEnable()
    {
        if (_xrInteract != null)
        {
            _xrInteract.hoverEntered.AddListener(OnHoverEntered);
            _xrInteract.hoverExited.AddListener(OnHoverExited);
            _xrInteract.selectEntered.AddListener(OnSelectEntered);
            _xrInteract.selectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDisable()
    {
        if (_xrInteract != null)
        {
            _xrInteract.hoverEntered.RemoveListener(OnHoverEntered);
            _xrInteract.hoverExited.RemoveListener(OnHoverExited);
            _xrInteract.selectEntered.RemoveListener(OnSelectEntered);
            _xrInteract.selectExited.RemoveListener(OnSelectExited);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRMultiplayer.MiniGames;

public class PlayPile : MonoBehaviour
{
    [SerializeField] protected MiniGame_Cards mg_cards;

    private void OnTriggerEnter(Collider other)
    {
        mg_cards.PlayCard(other.gameObject);
    }
}

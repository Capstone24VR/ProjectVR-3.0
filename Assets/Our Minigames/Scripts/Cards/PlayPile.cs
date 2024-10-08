using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRMultiplayer.MiniGames;

public class PlayPile : MonoBehaviour
{
    [SerializeField] protected NetworkedCards m_NetworkedGameplay;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<Card>().played)
        {
            m_NetworkedGameplay.PlayCard(other.gameObject);
        }
    }
}

using UnityEngine;
using Unity.Netcode;
using XRMultiplayer.MiniGames;


public class PlayPile : MonoBehaviour
{
    [SerializeField] protected NetworkedCards m_NetworkedGameplay;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<Card>().played)
        {
            m_NetworkedGameplay.RequestPlayCard(other.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }
}

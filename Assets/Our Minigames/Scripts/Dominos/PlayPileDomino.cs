using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using XRMultiplayer.MiniGames;

public class PlayPileDomino : MonoBehaviour
{
    [SerializeField] protected NetworkedDomino m_NetworkedGameplay;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"the name: {other.gameObject.name}\t\tthe tag: {other.gameObject.tag}");
        //if ()
        //{
        //    m_NetworkedGameplay.RequestPlayCard(other.GetComponent<NetworkObject>().NetworkObjectId);
        //}
    }
}

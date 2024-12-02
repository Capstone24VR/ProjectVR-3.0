using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRMultiplayer.MiniGames;

public class PlayPileDomino : MonoBehaviour
{
    [SerializeField] protected NetworkedDomino m_NetworkedGameplay;

    public bool firstDominoPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        //if (other.CompareTag("Domino") && !firstDominoPlayed)
        //{
        //    XRGrabInteractable grabInteractable = other.GetComponentInParent<XRGrabInteractable>();
        //    if (grabInteractable != null)
        //    {
        //        // Find the current interactor holding the object
        //        var interactor = grabInteractable.interactorsSelecting.Count > 0
        //            ? grabInteractable.interactorsSelecting[0]
        //            : null;

        //        // Force the drop
        //        if (interactor != null)
        //        {
        //            interactor.f
        //            interactor.interactionManager.CancelInteractable(grabInteractable);
        //        }

        //        grabInteractable.enabled = false; // Optionally disable to prevent re-grab

        //        m_NetworkedGameplay.RequestPlayFirstDomino(other.GetComponentInParent<NetworkObject>().NetworkObjectId);
        //    }
        //}    
    }
}

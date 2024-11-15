using UnityEngine;
using System.Collections.Generic;
using Domino;
using XRMultiplayer.MiniGames;

public class SnapManager : MonoBehaviour
{
    public List<HitboxComponent> hitboxes; // List of all hitbox components in the scene
    public Color highlightColor = new Color(0, 0, 1, 0.5f); // Color for highlighting active hitboxes
    public Color defaultColor = Color.clear; // Default transparent color for hitboxes

    private HitboxComponent activeHitbox = null; // The currently active hitbox being highlighted
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable currentDomino; // Domino being grabbed
    private NetworkedDomino currentNetwork;

    private void Awake()
    {
        // Initialize the networking component if applicable
        currentNetwork = FindAnyObjectByType<NetworkedDomino>();
        if (currentNetwork == null)
        {
            Debug.LogError("NetworkedDomino component not found. Multiplayer functionality might be affected.");
        }
    }

    private void Start()
    {
        // Set all hitboxes to the default color at the start
        foreach (var hitbox in hitboxes)
        {
            hitbox.SetColor(defaultColor);
        }
    }

    public void HighlightHitbox(HitboxComponent hitbox, UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable domino)
    {
        // Highlight the hitbox only if it's not the currently active one
        if (activeHitbox != hitbox)
        {
            if (activeHitbox != null)
            {
                // Reset the previous hitbox's color
                activeHitbox.SetColor(defaultColor);
            }

            activeHitbox = hitbox;
            currentDomino = domino;

            // Highlight the new active hitbox
            activeHitbox.SetColor(highlightColor);
        }
    }

    public void ResetHighlight(HitboxComponent hitbox)
    {
        // Reset the color of the hitbox when it is no longer active
        if (activeHitbox == hitbox)
        {
            activeHitbox.SetColor(defaultColor);
            activeHitbox = null;
            currentDomino = null;
        }
    }
}

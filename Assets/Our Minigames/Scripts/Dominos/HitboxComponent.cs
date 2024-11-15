using Unity.Netcode;
using UnityEngine;
using XRMultiplayer.MiniGames;

namespace Domino
{
    public class HitboxComponent : MonoBehaviour
    {
        public bool isUsed = false; // Indicates if this hitbox is already used for snapping
        private Renderer hitboxRenderer;
        private SnapManager snapManager;
        public NetworkedDomino m_MiniGame;

        // Enum to select which side this hitbox represents
        public enum Side
        {
            Top,
            Bottom
        }

        // Expose the Side selection in the Inspector
        public Side hitboxSide;

        // Variable to store the side's value based on the selection
        public int sideValue;
        public int hitboxindex;

        private void Awake()
        {
            m_MiniGame = FindAnyObjectByType<NetworkedDomino>();
            hitboxRenderer = GetComponent<Renderer>();
            snapManager = GetComponentInParent<SnapManager>();

            // Set the initial color of the hitbox to fully transparent
            if (hitboxRenderer != null)
            {
                Color transparentColor = hitboxRenderer.material.color;
                transparentColor.a = 0f; // Fully transparent
                hitboxRenderer.material.color = transparentColor;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // Check if the other collider belongs to a domino
            if (other.CompareTag("Domino"))
            {
                var otherGrabInteractable = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                var otherDominoData = other.GetComponentInParent<Domino_data>();

                // Ensure the domino has the required components and the hitbox is not already used
                if (otherGrabInteractable != null && otherDominoData != null && !isUsed)
                {
                    // Check if the sideValue matches either side of the domino
                    if (sideValue == otherDominoData.Top_side || sideValue == otherDominoData.But_side)
                    {
                        // Highlight the hitbox in green to indicate a match
                        SetColor(Color.green);

                        // Snap only when the domino is released (isSelected becomes false)
                        if (!otherGrabInteractable.isSelected)
                        {

                            //SnapDomino(other.transform);
                            Debug.Log("sssss");
                            m_MiniGame.RequestPlayCard(other.GetComponent<NetworkObject>().NetworkObjectId, GetComponentInParent<NetworkObject>().NetworkObjectId, hitboxindex);
                        }
                    }
                    else
                    {
                        // Highlight the hitbox in red if there's no match
                        SetColor(Color.red);

                    }

                    // Notify the SnapManager of the active hitbox and domino
                    snapManager.HighlightHitbox(this, otherGrabInteractable);
                }
            }
        }


        private void OnTriggerExit(Collider other)
        {
            // Reset the color to transparent when the domino leaves the hitbox
            SetColor(new Color(1, 1, 1, 0)); // Fully transparent
        }

        public void SetColor(Color color)
        {
            if (hitboxRenderer != null)
            {
                SetMaterialOpaque(); // Ensure the material is opaque before changing color
                color.a = 1f; // Ensure the new color is fully opaque
                hitboxRenderer.material.color = color;
            }
            else
            {
                Debug.LogError("Renderer component is missing. Cannot set color.");
            }
        }


        // Method to set the side's value based on the enum selection
        public void SetSideValue(int topSide, int bottomSide)
        {
            sideValue = (hitboxSide == Side.Top) ? topSide : bottomSide;
        }

        public void SnapDomino(Transform domino)
        {
            // Snap the domino to the hitbox position and orientation
            domino.position = transform.position;
            domino.rotation = transform.rotation;

            // Mark this hitbox as used
            isUsed = true;

            // Disable grab functionality on the domino
            var grabInteractable = domino.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.enabled = false; // Disable the grab interactable to prevent further grabbing
            }
            else
            {
                Debug.LogWarning("XRGrabInteractable component is missing on the snapped domino.");
            }

            // Reset the color to transparent after snapping
            SetColor(new Color(1, 1, 1, 0));

        }

        public void SetMaterialOpaque()
        {
            if (hitboxRenderer != null)
            {
                Material material = hitboxRenderer.material;
                material.SetFloat("_Mode", 0); // Opaque mode
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 2000; // Default queue for opaque objects
            }
        }
    }
}

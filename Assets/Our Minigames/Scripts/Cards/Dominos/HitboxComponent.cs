using UnityEngine;


namespace Domino
{
    public class HitboxComponent : MonoBehaviour
    {
        public bool isUsed = false; // Indicates if this hitbox is already used for snapping
        private Renderer hitboxRenderer;
        private SnapManager snapManager;

        private void Awake()
        {
            hitboxRenderer = GetComponent<Renderer>();
            snapManager = GetComponentInParent<SnapManager>();
        }

        private void OnTriggerStay(Collider other)
        {
            // Check if the other collider belongs to a grabbed domino
            if (other.CompareTag("Domino"))
            {
                var otherGrabInteractable = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

                if (otherGrabInteractable != null && otherGrabInteractable.isSelected && !isUsed)
                {
                    snapManager.HighlightHitbox(this, otherGrabInteractable);
                }
            }
        }

        public void SetColor(Color color)
        {
            if (hitboxRenderer != null)
            {
                hitboxRenderer.material.color = color;
            }
        }
    }
}

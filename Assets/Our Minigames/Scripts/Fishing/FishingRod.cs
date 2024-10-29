using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using static FishAI;

public class FishingRod : MonoBehaviour
{
    public GameObject user;
    public GameObject water;
    public GameObject hook;
    public Transform rodTip;
    public Transform fishSpawnPoint;

    private myFishingLine fishLine;
    private FishingHook fishHook;
    public FishingMiniGame miniGame;

    public bool isUsing = false;
    public bool thumbBarPressed = false;

    private float hookSpeed = 0f;
    public bool hookChambered = false;
    public bool hookFlying = false;

    public Slider powerBar;
    public bool isCasting = false;
    private float castPower = 0f;
    public float castMultiplier = 5f;

    public bool isReeling = false;
    
    public Transform reelBox;
    public float reelPower = 0.5f;
    public float reelSize = 0.15f;

    private float reelPosition = 0.5f;
    private float oldReelChange = 0f;
    private float reelChange = 0f;

    public GameObject caughtItem = null;
    private Vector3 originalPos;

    // For tutorial Events
    public myEventsScripts tutorial;


    // Start is called before the first frame update
    private void Awake()
    {
        // Get the XRGrabInteractable component
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // Add event listeners for grab interactions
        grabbable.selectEntered.AddListener(Grabbed);
        grabbable.lastSelectExited.AddListener(Dropped);
        grabbable.activated.AddListener(Activated);
        grabbable.deactivated.AddListener(Deactivated);

        fishLine = GetComponentInChildren<myFishingLine>();
        fishHook = hook.GetComponent<FishingHook>();

        // Store the original position of the GameObject
        originalPos = transform.position;

    }

    private void Update()
    {
        if (isUsing)
        {

            if(!fishHook.caughtSomething)
            {
                miniGame.pause = true;
            }
            else
            {
                tutorial.miniGameFirstActivated.Value = true;
            }

            if (isCasting)
            {
                // Call the Casting method
                Casting();
            }

            if (isReeling)
            {
                reelPosition += reelChange * (reelPower + (user.GetComponent<PlayerStats>().reelLevel + 1)/7);
                reelPosition = Mathf.Clamp(reelPosition, reelSize/2, 1-reelSize/2);
                reelBox.position = Vector3.Lerp(miniGame.bottomPivot.position, miniGame.topPivot.position, reelPosition);
            }

            if (hook.transform.position.y <= water.transform.position.y)
            {
                // Disable hook flying behavior
                hookFlying = false;
                // Stop the hook's velocity
                hook.GetComponent<Rigidbody>().velocity = Vector3.zero;
                // Disable gravity for the hook
                hook.GetComponent<Rigidbody>().useGravity = false;
            }
        }
        else
        {
            miniGame.ResetBar();
            miniGame.gameObject.SetActive(false);
            miniGame.pause = true;
        }

        if (hook.activeSelf)
        {
            if (fishHook.caughtSomething)
            {
                // Store the caught object if the hook caught something
                caughtItem = fishHook.caughtObject;
            }
        }
    }


    public void Grabbed(SelectEnterEventArgs arg)
    {
        tutorial.rodFirstPickedUp.Value = true;


        // The object is being grabbed
        isUsing = true;
    }

    public void Dropped(SelectExitEventArgs arg)
    {
        // The object is no longer being grabbed
        isUsing = false;
        // Reset casting and line state
        ResetCast();
        ResetLine();
    }

    void Activated(ActivateEventArgs args)
    {
        if (hook.activeSelf && !hookFlying)
        {
            // Hide the power bar
            powerBar.gameObject.SetActive(false);
        }
        else
        {
            // Otherwise, set casting state and show power bar
            isCasting = true;
            powerBar.gameObject.SetActive(true);
        }
        Debug.Log("Trigger Button was pressed");
    }

    void Deactivated(DeactivateEventArgs args)
    {
        if (isUsing)
        {
            tutorial.rodFirstCasted.Value = true;


            // Store the power value when the object is being used
            castPower = powerBar.value;
        }
        if (hook.activeSelf)
        {
            if (fishHook.caughtSomething && caughtItem.GetComponent<FishAI>().state == FishState.Caught)
            {
                tutorial.caughtFirstFish.Value = true;
                string caughtName = caughtItem.name;
                caughtItem.transform.position = fishSpawnPoint.transform.position;
                caughtItem.transform.rotation = fishSpawnPoint.transform.rotation;
                caughtItem.AddComponent<Rigidbody>();
                caughtItem.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                caughtItem.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().useDynamicAttach = true;
                Debug.Log("I reeled in :" + caughtName);
            }
            hook.SetActive(false);
            miniGame.ResetBar();
            miniGame.gameObject.SetActive(false);
            fishHook.caughtSomething = false;
            fishHook.caughtObject = null;
        }
        else
        {
            hook.transform.position = rodTip.transform.position;
            hook.SetActive(true);
            hookFlying = true;
            hookSpeed = castPower * castMultiplier * (user.GetComponent<PlayerStats>().castLevel+1);
            hook.GetComponent<Rigidbody>().velocity = rodTip.transform.forward * hookSpeed;
            hook.GetComponent<Rigidbody>().useGravity = true;
        }
        ResetCast();
        powerBar.gameObject.SetActive(false);
        Debug.Log("Trigger Button was realeased");
    }

    private void CalculateCastSpeed()
    {

    }

    void Casting()
    {
        // Increment power bar value over time
        powerBar.value += Time.deltaTime;
    }

    public void Reel(float change)
    {
        if (miniGame.enabled)
        {
            isReeling = true;
            reelChange = change - oldReelChange;
            oldReelChange = change;
        }
    }

    public void SetReelingFalse()
    {
        reelChange = 0;
        isReeling = false;
    }


    public void ResetCast()
    {
        // Reset casting-related variables
        powerBar.value = 0;
        isCasting = false;
        powerBar.gameObject.SetActive(false);
    }

    public void ResetLine()
    {
        // Hide the hook
        fishHook.caughtObject = null;
        fishHook.caughtSomething = false;
        hook.SetActive(false);
    }

    public void TurnOffStruggleBar()
    {
        Invoke("TurnOffStruggleBarHelper", 2f);
    }

    private void TurnOffStruggleBarHelper()
    {
        miniGame.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Boundary")
        {
            // If colliding with a boundary, reset the position
            transform.position = originalPos;
        }
    }
}
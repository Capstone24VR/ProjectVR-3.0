using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;

public class FishingMiniGame : MonoBehaviour
{
    public GameObject struggleFish = null;
    private FishingRod rod;
    public Slider progressBar;

    public Transform topPivot;
    public Transform bottomPivot;
    public Transform fish;
    public Transform reelBox;
    public float reelSize = 17.85f;

    private float fishPosition;
    private float fishDestination;

    public float fishSpeed;
    public float fishTimer;
    public float timerMultipier = 3f;
    public float smoothMotion = 1f;

    public float rodDegredation = 0.05f;
    public float maxFailTimer = 10f;
    public float failTimer = 10f;

    public float maxWaitTime = 3f;
    private float waitTimer = 3f;
    public bool pause = false;

    private void Awake()
    {
        rod = GetComponentInParent<FishingRod>();
        ResetBar();
    }

    // Update is called once per frame
    void Update()
    {
        progressBar.GetComponentInChildren<TextMeshProUGUI>().text = "Progress: " + Mathf.RoundToInt(progressBar.value * 100) + "%";

        if (pause) { return; }
        Fish();
        ProgressCheck();
    }

    void Fish()
    {
        fishTimer -= Time.deltaTime;
        if (fishTimer < 0f)
        {
            fishTimer = Random.value * struggleFish.GetComponent<FishStats>().baitChance * timerMultipier;
            fishDestination = Random.value;
        }

        fishPosition = Mathf.SmoothDamp(fishPosition, fishDestination, ref fishSpeed, smoothMotion);
        fish.position = Vector3.Lerp(bottomPivot.position, topPivot.position, fishPosition);
    }

    void ProgressCheck()
    {
        
        if (failTimer < 0f)
        {
            Lose();
        }

        if (progressBar.value == 1)
        {
            Win();
        }

        float min = reelBox.localPosition.x - reelSize / 2;
        float max = reelBox.localPosition.x + reelSize / 2;

        if (fish.localPosition.x >= min && fish.localPosition.x <= max)
        {
            progressBar.value += rod.reelPower * Time.deltaTime;
        }
        else
        {
            progressBar.value -= (rodDegredation - (rodDegredation/2  * (1/8))) * Time.deltaTime;
        }
        if(progressBar.value == 0.0f)
        {
            failTimer -= Time.deltaTime;
        }
    }

    public void ResetBar()
    {
        failTimer = maxFailTimer;
        reelBox.localPosition = new Vector3(0, reelBox.localPosition.y, reelBox.localPosition.z);
        fish.localPosition = new Vector3(0, fish.localPosition.y, fish.localPosition.z);
        progressBar.value = 0f;
        transform.Find("MiniGame").GetComponentInChildren<TextMeshProUGUI>().text = "Move the green box so the fish is inside";
        waitTimer = 0f;
    }


    void Win()
    {
        pause = true;
        struggleFish.GetComponent<FishAI>().state = FishAI.FishState.Caught;
        transform.Find("MiniGame").GetComponentInChildren<TextMeshProUGUI>().text = "Press the [ACTIVATE] button to reel the fish in!";
        Debug.Log("Fishcaught");
    }

    void Lose()
    {
        pause = true;
        struggleFish.GetComponent<FishAI>().state = FishAI.FishState.Wander;
        struggleFish.GetComponent<FishStats>().resistance = Mathf.Min(struggleFish.GetComponent<FishStats>().resistance - 0.15f, 1f);
        struggleFish = null;
        rod.ResetLine();
        rod.TurnOffStruggleBar();
        transform.Find("MiniGame").GetComponentInChildren<TextMeshProUGUI>().text = "Aww, the fish slipped away";
        Debug.Log("You failed");
    }

    void releaseFish()
    {
        Lose();
        ResetBar();
        transform.Find("MiniGame").GetComponentInChildren<TextMeshProUGUI>().text = "You took too long :(";
    }
}

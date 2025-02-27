using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRMultiplayer.MiniGames;

public class NetworkedFishAI : NetworkBehaviour
{
    public enum FishState { Wander, Baited, Struggle, OnHook, Caught };
    public FishStats stats;

    public MiniGame_Fishing m_MiniGame;

    public List<Transform> hooks = new List<Transform>();
    public List<Transform> activeHooks;
    public int hookIndex;
    public Transform currentHook = null;

    public GameObject hookSpot;
    private float ogHeight = 0f;
    public float waterHeight = 0.25f;

    public Animator animator;
    private XRGrabInteractable _xrInteract;
    public Rigidbody rb;

    public bool baited;
    public float baitChance;
    private float baitTimer;
    private float waitTimer;
    private float wanderTimer;
    public float wanderDuration;
    public Vector3 target;
    public NetworkVariable<FishState> state = new NetworkVariable<FishState>();
    private bool changePos = true;

    // For Erratic Movement
    float minX = 0;
    float maxX = 0;
    float minZ = 0;
    float maxZ = 0;


    // For Wander Movement
    private float minPoolX = 0;
    private float maxPoolX = 0;
    private float minPoolZ = 0;
    private float maxPoolZ = 0;



    // Parameters
    public float minWanderDuration = 5f;
    public float maxWanderDuration = 20f;
    public float waitDuration = 2f; // Wait duration when reaching a destination
    public float maxIdleTime = 5f; // Maximum idle time before choosing a new random position
    public float maxBaitTime = 3f; // Maximum time before bait procs again

    private void Awake()
    {
        m_MiniGame = FindAnyObjectByType<MiniGame_Fishing>();

        foreach (var hook in m_MiniGame.m_Hooks)
        {
            hooks.Add(hook);
        }

        ogHeight = transform.position.y;
        target = transform.position;
        _xrInteract = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        stats = GetComponent<FishStats>();
        hookSpot = transform.Find("HookSpot").gameObject;
        gameObject.transform.localScale = Vector3.one * stats.weight;
        state.Value = FishState.Wander;
    }

     private void OnEnable()
    {
        if (_xrInteract != null)
        {
            _xrInteract.selectEntered.AddListener(OnGrab);
        }
    }

    private void OnDisable()
    {
        if (_xrInteract != null)
        {
            _xrInteract.selectEntered.RemoveListener(OnGrab);
        }
    }

    // Update is called once per fraame
    void Update()
    {
        if (IsServer)
        {
            ServerUpdate();
        }
    }

    private void ServerUpdate()
    {

        if (activeHooks.Count == 0 && state.Value != FishState.Caught)
        {
            if (state.Value != FishState.Wander)
            {
                baited = false;
                ChooseNewRandomposition();
            }
            Debug.Log("Erm");
            SetFishStateServerRpc(FishState.Wander);
        }
        switch (state.Value)
        {
            case FishState.Wander:
                Wander();
                break;
            case FishState.Baited:
                Baited();
                break;
            case FishState.Struggle:
                Struggle();
                break;
            case FishState.Caught:
                Caught();
                break;
        }
    }

    public void SetWanderArea(float minX, float maxX, float minZ, float maxZ)
    {
        minPoolX = minX;
        maxPoolX = maxX;
        minPoolZ = minZ;
        maxPoolZ = maxZ;
    }

    void Wander()
    {
        activeHooks = GetActiveHooks();
        if (activeHooks.Count > 0)
        {
            baitTimer += Time.deltaTime;
            if (baitTimer >= maxBaitTime)
            {
                hookIndex = isBaited(activeHooks);
                if (hookIndex > -1)
                {
                    baited = true;
                }
                if (baited)
                {
                    Debug.Log($"{name} has been baited to rod: {activeHooks[hookIndex].parent.parent.name}");
                    SetFishStateServerRpc(FishState.Baited);
                }
                baitTimer = 0f;
            }
        }
        //// Move towards the target position
        MoveServerRpc(target);

        wanderTimer += Time.deltaTime;

        // Check if reached the target position
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitDuration)
            {
                ChooseNewRandomposition();
            }
        }

        if (wanderTimer > wanderDuration)
        {
            ChooseNewRandomposition();
        }
    }

    int isBaited(List<Transform> activeHooks)
    {
        SortHooksByDistance(activeHooks);

        int index = -1;
        foreach (var hook in activeHooks)
        {
            index++;
            if (hook.transform.position.y <= waterHeight && !hook.GetComponent<FishingHook>().caughtSomething.Value)
            {
                var distanceFromHook = Vector3.Distance(gameObject.transform.position, hook.transform.position);
                var cap = 10 / distanceFromHook + stats.weight / 2;
                var roll = Random.Range(0f, cap);
                if (roll <= cap / (stats.resistance + 1))
                {
                    return index;
                }
            }
        }

        return index;
    }

    List<Transform> GetActiveHooks()
    {
        var res = new List<Transform>(hooks);

        for(int i = res.Count-1; i >= 0; i--)
        {
            if (res[i].transform.position.y > waterHeight && !res[i].GetComponent<FishingHook>().caughtSomething.Value)
            {
                res.Remove(res[i]);
            }
        }
        return res;
    }


    void Baited()
    {
        currentHook = activeHooks[hookIndex];
        activeHooks = GetActiveHooks();

        if (currentHook.GetComponent<FishingHook>().caughtSomething.Value && currentHook.GetComponent<FishingHook>().caughtObject != this || !activeHooks.Contains(currentHook))
        {
            Debug.Log($"{name} is no longer baited");
            hookIndex = -1;
            ChooseNewRandomposition();
            SetFishStateServerRpc(FishState.Wander);
        }
        else
        {
            target = currentHook.transform.position;
            MoveServerRpc(target);
        }
    }


    private void SortHooksByDistance(List<Transform> list)
    {
        list.Sort((hook1, hook2) =>
        {
            float distanceToHook1 = Vector3.Distance(hook1.position, transform.position);
            float distanceToHook2 = Vector3.Distance(hook1.position, transform.position);
            return distanceToHook1.CompareTo(distanceToHook2);
        });
    }

    void Struggle()
    {
        if(currentHook == null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;


            if(transform.position.y < waterHeight)
            {
                Debug.Log("Struggle to wander");
                SetFishStateServerRpc(FishState.Wander);
            }
            return;
        }


        if (currentHook.GetComponent<FishingHook>().rodDropped.Value)
        {
            Debug.Log("IOMG");
            _xrInteract.enabled = true;
            rb.useGravity = true;
            rb.isKinematic = false;

            currentHook.GetComponent<FishingHook>().caughtSomething.Value = false;
            currentHook.GetComponent<FishingHook>().caughtObject = null;
            currentHook = null;

            SetFishStateServerRpc(FishState.Caught);
        }
        else
        {
            if (transform.position.y >= waterHeight)
            {
                _xrInteract.enabled = true;
            }
            else
            {
                _xrInteract.enabled = false;
            }
            MoveServerRpc(currentHook.position);
        }
        //ErraticMove(3f);
    }

    void ErraticMove(float distance)
    {
        if (changePos)
        {
            minX = transform.position.x - distance;
            maxX = transform.position.x + distance;
            minZ = transform.position.z - distance / 2;
            maxZ = transform.position.z + distance / 2;
            changePos = false;
        }
        MoveServerRpc(target);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            // Choose a new random nearby position within specified x and z positions
            target = new Vector3(Random.Range(minX, maxX), ogHeight, Random.Range(minZ, maxZ));
        }
    }

    void Caught()
    {
        rb.useGravity = true;
        rb.isKinematic = false;
        _xrInteract.enabled = true;

        if (transform.position.y < waterHeight - 1)
        {
            ResetFish();
            SetFishStateServerRpc(FishState.Wander);
        }
    }


    void ResetFish()
    {
        rb.useGravity = false;
        rb.isKinematic = false;
        _xrInteract.enabled = false;
        rb.velocity = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    //void Startled()
    //{
    //    Vector3 direction = (hook.transform.position - transform.position).normalized;
    //    target = target - direction;
    //    waitTimer = 0f;
    //    wanderTimer = 0f;
    //    state.Value = FishState.Wander;
    //}

    [ServerRpc]
    private void MoveServerRpc(Vector3 target)
    {
        var step = stats.speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, step);

        MoveClientRpc(transform.position, target);
    }

    [ClientRpc]
    private void MoveClientRpc(Vector3 newPosition, Vector3 lookAt)
    {
        transform.position = newPosition;
        transform.LookAt(lookAt);
    }

    [ServerRpc(RequireOwnership=false)]
    public void SetFishStateServerRpc(FishState newState)
    {
        state.Value = newState;
    }


    private void ChooseNewRandomposition()
    {
        // Choose a new random position within specified x and z positions
        target = new Vector3(Random.Range(minPoolX, maxPoolX), ogHeight, Random.Range(minPoolZ, maxPoolZ));
        wanderDuration = Random.Range(minWanderDuration, maxWanderDuration);
        waitTimer = 0f;
        wanderTimer = 0f;
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        Debug.Log($"{gameObject.name} was grabbed.");
        currentHook.GetComponent<FishingHook>().caughtObject = null;
        currentHook.GetComponent<FishingHook>().caughtSomething.Value = false;
        currentHook.GetComponentInParent<NewFishingRod>().ResetCast();
        currentHook = null;
        rb.useGravity = true;
        rb.isKinematic = true;
        SetFishStateServerRpc(FishState.Caught);   
    }

    [ServerRpc(RequireOwnership = false)]
    void DestroyServerRpc()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Water" && (state.Value == FishState.Struggle || state.Value == FishState.Caught))
        {
            Debug.Log("Somehow touched water trigger");
            ResetFish();
            SetFishStateServerRpc(FishState.Wander);
        }

        if (other.gameObject.tag == "Cooler")
        {
            //other.gameObject.GetComponent<Cooler>().newFish = this;
            other.gameObject.GetComponent<Cooler>().OnFishCatch(NetworkObjectId);

            m_MiniGame.LocalPlayerScored((int)(stats.weight * stats.multiplier * 100));

            DestroyServerRpc();
        }
    }
}
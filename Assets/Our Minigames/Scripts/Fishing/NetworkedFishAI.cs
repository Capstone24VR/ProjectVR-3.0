using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using XRMultiplayer.MiniGames;

public class NetworkedFishAI : NetworkBehaviour
{
    public enum FishState { Wander, Baited, Struggle, Caught };
    public FishStats stats;

    public MiniGame_Fishing m_MiniGame;

    public List<Transform> hooks = new List<Transform>();
    public List<Transform> activeHooks;
    public int hookIndex;

    public GameObject hookSpot;
    public float waterHeight = 0.25f;

    public Animator animator;

    public bool baited;
    public float baitChance;
    private float baitTimer;
    private float waitTimer;
    private float wanderTimer;
    public float wanderDuration;
    public Vector3 target;
    public FishState state;
    public NetworkVariable<FishState> networkedState = new NetworkVariable<FishState>();
    private bool changePos = true;

    // For Erratic Movement
    float minX = 0;
    float maxX = 0;
    float minZ = 0;
    float maxZ = 0;



    // Parameters
    public float minWanderDuration = 5f;
    public float maxWanderDuration = 20f;
    public float waitDuration = 2f; // Wait duration when reaching a destination
    public float maxIdleTime = 5f; // Maximum idle time before choosing a new random position
    public float maxBaitTime = 3f; // Maximum time before bait procs again

    private void Awake()
    {
        m_MiniGame = FindAnyObjectByType<MiniGame_Fishing>();

        Debug.Log(m_MiniGame.name);
        foreach (var hook in m_MiniGame.m_Hooks)
        {
            hooks.Add(hook);
        }

        target = transform.position;
        animator = GetComponentInChildren<Animator>();
        stats = GetComponent<FishStats>();
        hookSpot = transform.Find("HookSpot").gameObject;
        gameObject.transform.localScale = Vector3.one * stats.weight;
        state = FishState.Wander;
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

        if (activeHooks.Count == 0 && state != FishState.Caught)
        {
            if (state != FishState.Wander)
            {
                baited = false;
                ChooseNewRandomposition();
            }
            SetFishStateServerRpc(FishState.Wander);
        }
        switch (state)
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
                //Caught();
                break;
        }
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
    void Struggle()
    {
        ErraticMove(3f);
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
            target = new Vector3(Random.Range(minX, maxX), transform.position.y, Random.Range(minZ, maxZ));
        }
    }

    int isBaited(List<Transform> activeHooks)
    {
        SortHooksByDistance(activeHooks);

        int index = -1;
        foreach (var hook in activeHooks)
        {
            index++;
            if (hook.transform.position.y <= waterHeight && hook.GetComponent<FishingHook>().caughtSomething.Value)
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
            if (res[i].transform.position.y > waterHeight)
            {
                res.Remove(res[i]);
            }
        }
        return res;
    }


    void Baited()
    {
        activeHooks = GetActiveHooks();
        //if (hooks[hookIndex].GetComponent<FishingHook>().caughtSomething.Value && hooks[hookIndex].GetComponent<FishingHook>().caughtObject != this)
        //{
        //    ChooseNewRandomposition();
        //    SetFishStateServerRpc(FishState.Wander);
        //}
        //else
        //{
        //    target = hooks[hookIndex].transform.position;
        //    MoveServerRpc(target);
        //}
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

    //void Startled()
    //{
    //    Vector3 direction = (hook.transform.position - transform.position).normalized;
    //    target = target - direction;
    //    waitTimer = 0f;
    //    wanderTimer = 0f;
    //    state = FishState.Wander;
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

    [ServerRpc]
    public void SetFishStateServerRpc(FishState newState)
    {
        state = newState;
        networkedState.Value = newState;
    }

    private void ChooseNewRandomposition()
    {
        // Choose a new random position within specified x and z positions
        target = new Vector3(Random.Range(-20f, 25f), transform.position.y, Random.Range(-77f, -32f));
        wanderDuration = Random.Range(minWanderDuration, maxWanderDuration);
        waitTimer = 0f;
        wanderTimer = 0f;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Cooler")
        {
            //other.gameObject.GetComponent<Cooler>().newFish = this;
            other.gameObject.GetComponent<Cooler>().OnFishCatch();

            MiniGame_Fishing m_MiniGame = FindAnyObjectByType<MiniGame_Fishing>();

            m_MiniGame.LocalPlayerScored((int)(stats.weight * stats.multiplier * 100));

            Destroy(gameObject);
        }
    }
}
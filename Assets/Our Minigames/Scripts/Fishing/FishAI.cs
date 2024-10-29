using UnityEngine;

public class FishAI : MonoBehaviour
{
    public enum FishState { Wander, Baited, Struggle, Caught};
    public FishStats stats;

    public GameObject hook;
    public GameObject water;
    public GameObject hookSpot;

    public GameObject fishSpawnPoint;
    public Animator animator;

    public myEventsScripts tutorial;

    public bool baited;
    public float baitChance;
    private float baitTimer;
    private float waitTimer;
    private float wanderTimer;
    public float wanderDuration;
    public Vector3 target;
    public FishState state;
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
        if (!hook.activeSelf && state != FishState.Caught)
        {
            if(state != FishState.Wander)
            {
                baited = false;
                ChooseNewRandomPosition();
            }
            state = FishState.Wander;
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
        if (hook.activeSelf == true && hook.transform.position.y <= water.transform.position.y && !hook.GetComponent<FishingHook>().caughtSomething)
        {
            baitTimer += Time.deltaTime;
            if (baitTimer >= maxBaitTime)
            {
                baited = isBaited();
                if (baited)
                {
                    Debug.Log(gameObject.name + " has been baited to the rod");
                    state = FishState.Baited;
                }
                baitTimer = 0f;
            }
        }
        // Move towards the target position
        Move(target);

        wanderTimer += Time.deltaTime;

        // Check if reached the target position
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitDuration)
            {
                ChooseNewRandomPosition();
            }
        }

        if (wanderTimer > wanderDuration)
        {   
            ChooseNewRandomPosition();
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
            minZ = transform.position.z - distance/2;
            maxZ = transform.position.z + distance/2;
            changePos = false;
        }
        Move(target);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            // Choose a new random nearby position within specified x and z positions
            target = new Vector3(Random.Range(minX, maxX), transform.position.y, Random.Range(minZ, maxZ));
        }
    }

    bool isBaited()
    {
        var distanceFromHook = Vector3.Distance(gameObject.transform.position, hook.transform.position);
        var cap = 10 / distanceFromHook + stats.weight/2;
        var roll = Random.Range(0f, cap);
        return roll <= (cap / (stats.resistance+1)); 
    }

    void Baited()
    {
        if (hook.GetComponent<FishingHook>().caughtSomething && hook.GetComponent<FishingHook>().caughtObject != gameObject)
        {
            ChooseNewRandomPosition();
            state = FishState.Wander;
        }
        else
        {
            target = hook.transform.position;
            Move(target);
        }
    }

    void Startled()
    {
        Vector3 direction = (hook.transform.position - transform.position).normalized;
        target = target - direction;
        waitTimer = 0f;
        wanderTimer = 0f;
        state = FishState.Wander;
    }

    private void Move(Vector3 target)
    {
        var step = stats.speed * Time.deltaTime;
        gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, target, step);
        gameObject.transform.LookAt(target);
    }

    private void ChooseNewRandomPosition()
    {
        // Choose a new random position within specified x and z positions
        target = new Vector3(Random.Range(-25f, 25f), transform.position.y, Random.Range(-25f, 25f));
        wanderDuration = Random.Range(minWanderDuration, maxWanderDuration);
        waitTimer = 0f;
        wanderTimer = 0f;
    }
     

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Cooler")
        {
            tutorial.soldFirstFish.Value = true;
            other.gameObject.GetComponent<Cooler>().newFish = this;
            other.gameObject.GetComponent<Cooler>().OnFishCatch();
            Destroy(gameObject);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using static UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents a networked version of the Whack-A-Pig mini game.
    /// </summary>
    public class NetworkedFishManager : NetworkBehaviour
    {
        /// <summary>
        /// The mini game to use for handling the mini game logic.
        /// </summary>
        MiniGame_Cards m_MiniGame;

        /// <summary>
        /// Whether the game has started
        /// </summary>
        [SerializeField] bool gameStarted = false;


        /// <summary>
        /// The parent object with the height where all fish spawn
        /// </summary>
        [SerializeField] Transform fishPool;

        public int maxFish = 30;
        public int currFish = 0;
        public bool gameStart = false;

        public float spawnTimer = 0f;
        public float maxSpawnTime = 5f;

        public GameObject[] fish = new GameObject[7];

        public BoxCollider waterBounds;

        public List<string> names = new List<string>();
        private float[] baitChanceArr = { .001f, .05f, .15f, .25f, .35f, .40f, .60f };
        private float totalChance = 1.721f;

        private float minSpawnX = 0;
        private float maxSpawnX = 0;
        private float minSpawnZ = 0;
        private float maxSpawnZ = 0;

        /// <summary>
        /// The current message routine being played.
        /// </summary>
        IEnumerator m_CurrentMessageRoutine;

        [SerializeField] protected List<GameObject> spawnedFishObject = new List<GameObject>();

        [SerializeField] protected NetworkList<NetworkObjectReference> _spawnedFish = new NetworkList<NetworkObjectReference>();

        [SerializeField] private MiniGameManager miniManager;

        void Start()
        {
            TryGetComponent(out m_MiniGame);
            _spawnedFish.OnListChanged += OnSpawnedFishChanged;

            totalChance = 0f;
            foreach (var chance in baitChanceArr)
            {
                totalChance += chance;
            }

            names.Add("Selase");
            names.Add("Tony");
            names.Add("Jake");
            names.Add("Pam");
            names.Add("Rick");
            names.Add("John");
            names.Add("Farquad");
            names.Add("Goku");
            names.Add("Sturdivant");
            names.Add("Chauh");
            names.Add("Thimble");
            names.Add("Enyo");
            names.Add("Rick");
            names.Add("Chindog");
            names.Add("Fabio");
            names.Add("Nii");
            names.Add("Sally");
            names.Add("Trish");
            names.Add("Dela");
            names.Add("Pubert");
            names.Add("Whitmer");
            names.Add("Packer");

            Bounds boxbounds = waterBounds.bounds;

            Debug.Log("Min: " + boxbounds.min);
            Debug.Log("Max: " + boxbounds.max);
            Debug.Log("Center: " + boxbounds.center);
            Debug.Log("Extents: " + boxbounds.extents);

            // Adjust spawn area to avoid edges, based on rotation
            Vector3[] colliderCorners = new Vector3[8];
            Vector3 center = waterBounds.transform.position;
            Vector3 size = waterBounds.size;

            // Calculate the 8 corners of the BoxCollider in world space
            colliderCorners[0] = center + waterBounds.transform.TransformDirection(new Vector3(size.x / 2, size.y / 2, size.z / 2)); // Top-Right-Front
            colliderCorners[1] = center + waterBounds.transform.TransformDirection(new Vector3(-size.x / 2, size.y / 2, size.z / 2)); // Top-Left-Front
            colliderCorners[2] = center + waterBounds.transform.TransformDirection(new Vector3(-size.x / 2, size.y / 2, -size.z / 2)); // Top-Left-Back
            colliderCorners[3] = center + waterBounds.transform.TransformDirection(new Vector3(size.x / 2, size.y / 2, -size.z / 2)); // Top-Right-Back
            colliderCorners[4] = center + waterBounds.transform.TransformDirection(new Vector3(size.x / 2, -size.y / 2, size.z / 2)); // Bottom-Right-Front
            colliderCorners[5] = center + waterBounds.transform.TransformDirection(new Vector3(-size.x / 2, -size.y / 2, size.z / 2)); // Bottom-Left-Front
            colliderCorners[6] = center + waterBounds.transform.TransformDirection(new Vector3(-size.x / 2, -size.y / 2, -size.z / 2)); // Bottom-Left-Back
            colliderCorners[7] = center + waterBounds.transform.TransformDirection(new Vector3(size.x / 2, -size.y / 2, -size.z / 2)); // Bottom-Right-Back

            // Find min and max points based on corners
            minSpawnX = Mathf.Min(colliderCorners[0].x, colliderCorners[1].x, colliderCorners[2].x, colliderCorners[3].x, colliderCorners[4].x, colliderCorners[5].x, colliderCorners[6].x, colliderCorners[7].x);
            maxSpawnX = Mathf.Max(colliderCorners[0].x, colliderCorners[1].x, colliderCorners[2].x, colliderCorners[3].x, colliderCorners[4].x, colliderCorners[5].x, colliderCorners[6].x, colliderCorners[7].x);

            minSpawnZ = Mathf.Min(colliderCorners[0].z, colliderCorners[1].z, colliderCorners[2].z, colliderCorners[3].z, colliderCorners[4].z, colliderCorners[5].z, colliderCorners[6].z, colliderCorners[7].z);
            maxSpawnZ = Mathf.Max(colliderCorners[0].z, colliderCorners[1].z, colliderCorners[2].z, colliderCorners[3].z, colliderCorners[4].z, colliderCorners[5].z, colliderCorners[6].z, colliderCorners[7].z);

            // Apply offset to avoid spawning too close to the edges
            minSpawnX += 2;
            maxSpawnX -= 2;
            minSpawnZ += 2;
            maxSpawnZ -= 2;
        }

        private void Update()
        {
            currFish = fishPool.childCount;
        }


        IEnumerator WaitForClientConnection()
        {
            while (!NetworkManager.Singleton.IsClient || !NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("Waiting for client connection...");
                yield return new WaitForSeconds(0.5f); // Wait until the client is connected
            }

            Debug.Log("Client is now connected.");
            NotifyFishReadyClientRpc(); // Notify clients once they are connected
        }


        [ClientRpc]
        private void NotifyFishReadyClientRpc()
        {
            Debug.Log("Fish Pool is ready for interaction.");
        }



        public IEnumerator ResetGame()
        {
            StopAllCoroutines();
            RemoveSpawnedFish();

            yield return new WaitForSeconds(0.5f); // Give time for clients to catch up

            if (IsServer)
            {
                StartCoroutine(WaitForClientConnection());
            }
        }

        public void EndGame()
        {
            StopAllCoroutines();
            StopSpawningFish();
            RemoveSpawnedFish();
        }

        public IEnumerator SpawnFishLoop()
        {
            while(gameStart)
            {
                SpawnProcessServer();
                float newSpawnTime = UnityEngine.Random.Range(.5f, maxSpawnTime);
                yield return new WaitForSeconds(newSpawnTime);
            }
        }

        public void StartSpawningFish()
        {
            if (IsServer)
            {
                gameStart = true;
                StartCoroutine(SpawnFishLoop());
            }
        }

        public void StopSpawningFish()
        {
            gameStart = false;
            StopCoroutine(SpawnFishLoop());
        }

        /// <summary>
        /// Spawns a fish on the server.
        /// </summary>
        public void SpawnProcessServer()
        {
            if (IsServer)
            {
                currFish = fishPool.transform.childCount;
                if (currFish < maxFish)
                {

                    float currentCheck = 0f;
                    int type = 0;

                    float roll = UnityEngine.Random.Range(0f, totalChance);
                    foreach (float fishChance in baitChanceArr)
                    {
                        currentCheck += fishChance;

                        if (roll <= currentCheck)
                        {
                            break;
                        }

                        else if (currentCheck == totalChance)
                        {
                            break;
                        }
                        type++;
                    }

                    int name = UnityEngine.Random.Range(0, names.Count);
                    Vector3 spawnPoint = new Vector3(UnityEngine.Random.Range(minSpawnX, maxSpawnX), fishPool.position.y, UnityEngine.Random.Range(minSpawnZ, maxSpawnZ));

                    var spawn = Instantiate(fish[type], spawnPoint, Quaternion.identity, fishPool);
                    spawn.transform.localScale = Vector3.one * spawn.GetComponent<FishStats>().weight;
                    spawn.GetComponent<NetworkedFishAI>().SetWanderArea(minSpawnX, maxSpawnX, minSpawnZ, maxSpawnZ);


                    spawn.name = names[name] + " the " + fish[type].name;
                    spawn.SetActive(true);


                    NetworkObject networkObject = spawn.GetComponent<NetworkObject>();

                    if (networkObject != null)
                    {
                        networkObject.Spawn();
                        Debug.Log($"{spawn.name} has been spawned with id of: {networkObject.NetworkObjectId}");
                        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(networkObject.NetworkObjectId))
                        {
                            Debug.LogError($"Failed to register {spawn.name} with NetworkObjectId {networkObject.NetworkObjectId}");
                            return;
                        }
                    }
                    else
                    {
                        Debug.LogError("NetworkObject component is missing on the fish object.");
                        return;
                    }

                    _spawnedFish.Add(networkObject);

                    float waitTime = UnityEngine.Random.Range(3f, maxSpawnTime);
                    SpawnProcessClientRpc(waitTime, spawnPoint, spawn.name, networkObject.NetworkObjectId);
                }
            }
        }


        [ClientRpc]
        public void SpawnProcessClientRpc(float waitTime, Vector3 position, string name, ulong networkObjectId)
        {
            StartCoroutine(SpawnNewFish(waitTime, position, name, networkObjectId));
        }

        IEnumerator SpawnNewFish(float time, Vector3 position, string name, ulong networkObjectId)
        {
            yield return new WaitForSeconds(.25f);

            NetworkObject spawn = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
            spawn.TrySetParent(fishPool, false);
            spawn.transform.parent = fishPool;
            spawn.transform.localScale = Vector3.one * spawn.GetComponent<FishStats>().weight;
            spawn.transform.position = position;
            spawn.name = name;
            spawn.gameObject.SetActive(true);
            Debug.Log($"{spawn.name} has spawned on the clients");

            yield return new WaitForSeconds(time);

        }



        private void RemoveSpawnedFish()
        {
            if (IsServer)
            {
                // Notify clients to clear their hands and card visuals
                ClearAllFishClientRpc();


                // Clear the piles
                spawnedFishObject.Clear();

                foreach (NetworkObjectReference fishReference in _spawnedFish)
                {
                    if (fishReference.TryGet(out NetworkObject fish) && fish.IsSpawned)
                    {
                        fish.Despawn(true); // Despawn the card across the network
                    }
                }
                _spawnedFish.Clear();

                currFish = 0;
            }
        }


        [ClientRpc]
        private void ClearAllFishClientRpc()
        {
            spawnedFishObject.Clear();
        }

        private void OnSpawnedFishChanged(NetworkListEvent<NetworkObjectReference> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<NetworkObjectReference>.EventType.Add:
                    // A new card was added to the draw pile
                    Debug.Log($"Fish added: {changeEvent.Value}");
                    if (changeEvent.Value.TryGet(out NetworkObject noA))
                    {
                        spawnedFishObject.Add(noA.gameObject);
                    }
                    break;

                case NetworkListEvent<NetworkObjectReference>.EventType.Remove:
                    // A card was removed from the draw pile
                    Debug.Log($"Fish removed: {changeEvent.Value}");
                    if (changeEvent.Value.TryGet(out NetworkObject noR))
                    {
                        spawnedFishObject.Remove(noR.gameObject);
                    }
                    break;
            }
        }
    }
}

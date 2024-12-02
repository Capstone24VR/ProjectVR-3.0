using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Domino_data;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Domino;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents a networked version of the Whack-A-Pig mini game.
    /// </summary>
    public class NetworkedDomino : NetworkBehaviour
    {
        /// <summary>
        /// The hands to use for playing.
        /// </summary>
        [SerializeField] NetworkedHandDomino[] m_hands;

        /// <summary>
        /// The Basic Domino (Not Doubles) prefab to spawn.
        /// </summary>
        [SerializeField] GameObject dominoBasic;

        /// <summary>
        /// The Domino (Double) prefab to spawn.
        /// </summary>
        [SerializeField] GameObject dominoDouble;

        /// <summary>
        /// The card prefab to spawn.
        /// </summary>
        [SerializeField] GameObject drawPileObj;

        /// <summary>
        /// The card prefab to spawn.
        /// </summary>
        [SerializeField] GameObject playPileObj;

        /// <summary>
        /// The mini game to use for handling the mini game logic.
        /// </summary>
        MiniGame_Domino m_MiniGame;

        /// <summary>
        /// Whether the game has started
        /// </summary>
        [SerializeField] bool gameStarted = false;

        /// <summary>
        /// The current message routine being played.
        /// </summary>
        IEnumerator m_CurrentMessageRoutine;

        /// <summary>
        /// Keeps track of the clients who have completed deck creation
        /// </summary>
        private HashSet<ulong> clientsThatHaveCompletedDeck = new HashSet<ulong>();

        /// <summary>
        /// The number of starting cards
        /// </summary>
        [SerializeField] int startingHand = 5;


        // To do Add Rounds


        [SerializeField] protected NetworkList<NetworkObjectReference> deck = new NetworkList<NetworkObjectReference>();
        [SerializeField] protected List<GameObject> drawObject = new List<GameObject>();
        [SerializeField] protected List<GameObject> playObject = new List<GameObject>();
        [SerializeField] protected List<int> playSidesList = new List<int>();

        [SerializeField] protected NetworkList<NetworkObjectReference> _drawPile = new NetworkList<NetworkObjectReference>();
        [SerializeField] protected NetworkList<NetworkObjectReference> _playPile = new NetworkList<NetworkObjectReference>();
        [SerializeField] protected NetworkList<int> _playSides = new NetworkList<int>();

        [SerializeField] protected int currentHandIndex;

        [SerializeField] protected List<NetworkedHandDomino> activeHands = new List<NetworkedHandDomino>();

        [SerializeField] private MiniGameManager miniManager;

        void Start()
        {
            TryGetComponent(out m_MiniGame);
            deck.OnListChanged += OnDeckChanged;
            _drawPile.OnListChanged += OnDrawPileChanged;
            _playPile.OnListChanged += OnPlayPileChanged;
            _playSides.OnListChanged += OnPlayableSidesChanged;

            for (int i = 0; i < m_hands.Length; i++)
            {
                m_hands[i].ownerManager.seatHandler.handIndex = i;
                m_hands[i].ownerManager.seatHandler.OnTriggerAction += TriggerReadyState;
            }
        }

        void TriggerReadyState(Collider other, bool entered, int handIndex)
        {
            if (other.TryGetComponent(out CharacterController controller))
            {
                ToggleHandReadyServerRpc(entered, handIndex);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void ToggleHandReadyServerRpc(bool isReady, int index)
        {
            Debug.Log($"Server recieved request: Toggling Hand {index} to {isReady}");
            if (!gameStarted)
            {
                m_hands[index].active = isReady;
                ToggleHandReadyClientRpc(isReady, index);
            }
        }

        [ClientRpc]
        void ToggleHandReadyClientRpc(bool isReady, int index)
        {
            Debug.Log($"Synching Clients: toggling Hand {index} to {isReady}");
            m_hands[index].active = isReady;
        }


        IEnumerator WaitForClientConnection()
        {
            while (!NetworkManager.Singleton.IsClient || !NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("Waiting for client connection...");
                yield return new WaitForSeconds(0.5f); // Wait until the client is connected
            }

            Debug.Log("Client is now connected.");
            NotifyDeckReadyClientRpc(); // Notify clients once they are connected
        }


        [ClientRpc]
        private void NotifyDeckReadyClientRpc()
        {
            Debug.Log("Deck is ready for interaction.");
        }



        public IEnumerator ResetGame()
        {
            ToggleTeleportbuttonsClientRpc(true);
            StopAllCoroutines();
            RemoveGeneratedDominoesServer();
            gameStarted = false;

            Debug.Log($"Playable sides is clear (count: {_playSides.Count})");

            yield return new WaitForSeconds(0.5f); // Give time to remove all cards

            activeHands.Clear();

            yield return new WaitForSeconds(0.5f); // Give time for clients to catch u

            if (IsServer)
            {
                StartCoroutine(WaitForClientConnection());
            }
        }

        public void StartGame()
        {
            GetActiveHandsServer();
            CreateDeckServer();
            ShuffleDeckServer();
            InstantiateDrawPileServer();
            ToggleTeleportbuttonsClientRpc(false);

            StartCoroutine(WaitForClientsToCreateDeck());
        }

        private IEnumerator WaitForClientsToCreateDeck()
        {
            Debug.Log("Waiting for client's to finish deck creation . . . ");

            while (clientsThatHaveCompletedDeck.Count < miniManager.currentPlayerDictionary.Count)
            {
                yield return new WaitForSeconds(0.1f); // Short short delay for waiting
            }

            Debug.Log("All clients have finished creating their decks. Starting Game . . .");


            for (int i = 0; i < startingHand; i++)
            {
                foreach (NetworkedHandDomino hand in activeHands)
                {
                    if (hand.canDraw())
                    {
                        NetworkObjectReference topCard = _drawPile[_drawPile.Count - 1];

                        if (topCard.TryGet(out NetworkObject networkCard))
                        {
                            _drawPile.Remove(topCard);

                            if (!networkCard.IsSpawned)
                            {
                                networkCard.Spawn();
                            }
                            hand.DrawCardServerRpc(topCard);
                        }
                        else
                        {
                            Debug.Log("FATAL ERROR: Card not found at start");
                        }
                    }
                }
            }

            currentHandIndex = 0;
            StartDomino();
        }

        public void EndGame()
        {
            StopAllCoroutines();
            RemoveGeneratedDominoesServer();
            gameStarted = false;
        }

        [ClientRpc]
        private void ToggleTeleportbuttonsClientRpc(bool toggle)
        {
            foreach(var hand in m_hands)
            {
                hand.ownerManager.seatHandler.GetComponentInParent<TeleportationAnchor>().GetComponentInChildren<UIComponentToggler>().gameObject.SetActive(toggle);
            }

        }

        private void GetActiveHandsServer()
        {
            if (IsServer)
            {
                List<int> activeIndex = new List<int>();
                for (int i = 0; i < m_hands.Length; i++)
                {
                    if (m_hands[i].active)
                    {
                        activeHands.Add(m_hands[i]);
                        activeIndex.Add(i);
                    }
                }

                SyncActiveHandsClientRpc(activeIndex.ToArray());
            }
        }

        [ClientRpc]
        private void SyncActiveHandsClientRpc(int[] indexes)
        {
            List<NetworkedHandDomino> duplicate = new List<NetworkedHandDomino>(indexes.Length);
            foreach (var index in indexes)
            {
                duplicate.Add(m_hands[index]);
            }

            activeHands = duplicate;
        }



        /// <summary>
        /// Creates deck on the server.
        /// </summary>
        public void CreateDeckServer()
        {
            if (IsServer)
                StartCoroutine(CreateDeckOnServer());
        }

        IEnumerator CreateDeckOnServer()
        {
            Debug.Log("Creating Domino Deck ServerSide . . .");

            List<int> topSides = new List<int>();
            List<int> bottomSides = new List<int>();
            List<ulong> networkObjectIds = new List<ulong>();

            int dominosSpawned = 0;

            for (int topSide = 0; topSide < 7; topSide++)
            {
                for (int butSide = topSide; butSide < 7; butSide++)
                {
                    // Instantiate the domino prefab

                    GameObject newDomino = (butSide == topSide) ? Instantiate(dominoDouble, drawPileObj.transform, false) : Instantiate(dominoBasic, drawPileObj.transform, false);
                    var networkObject = newDomino.GetComponent<NetworkObject>();

                    if (networkObject != null)
                    {
                        networkObject.Spawn();
                        Debug.Log($"Domino [{topSide}-{butSide}] spawned with id of: {networkObject.NetworkObjectId}");
                        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(networkObject.NetworkObjectId))
                        {
                            Debug.LogError($"Failed to register Domino [{topSide}-{butSide}] with NetworkObjectId {networkObject.NetworkObjectId}");
                        }
                        dominosSpawned++;
                    }
                    else
                    {
                        Debug.LogError("NetworkObject component is missing on the domino prefab.");
                        continue; // Skip to the next iteration if no NetworkObject is found
                    }

                    // Ensure Domino_data component exists before assigning values and visual
                    Domino_data dominoComponent = newDomino.GetComponent<Domino_data>();
                    if (dominoComponent != null)
                    {
                        // Initialize the domino with the calculated prefab index
                        dominoComponent.InitializeDomino(topSide, butSide);
                        newDomino.name = $"Domino: [{topSide}-{butSide}]";

                        // Add domino data to lists for client notification
                        topSides.Add(topSide);
                        bottomSides.Add(butSide);
                        networkObjectIds.Add(networkObject.NetworkObjectId);

                        // Add domino to deck (NetworkList)
                        deck.Add(new NetworkObjectReference(networkObject));
                    }
                    else
                    {
                        Debug.LogError("Domino_data component is missing on the new domino prefab.");
                    }
                }
            }

            while (dominosSpawned < 28) // Total 28 dominos for a standard set
            {
                yield return null;  // Wait until next frame
            }

            Debug.Log("Domino deck created ServerSide. Notifying Clients . . .");
            // Notify clients with domino data
            CreateDeckClientRpc(topSides.ToArray(), bottomSides.ToArray(), networkObjectIds.ToArray());
        }



        // ClientRpc method with simple data types instead of custom struct
        [ClientRpc]
        public void CreateDeckClientRpc(int[] suits, int[] values, ulong[] networkObjectIds)
        {
            StartCoroutine(CreateDeckOnClient(suits, values, networkObjectIds));
        }

        IEnumerator CreateDeckOnClient(int[] topSides, int[] bottomSides, ulong[] networkObjectIds)
        {
            Debug.Log("Client received deck creation. Waiting for server to finish...");

            yield return new WaitForSeconds(1.0f); // Ensure enough delay for server to fully spawn cards

            Debug.Log("Creating Deck on Client...");

            for (int i = 0; i < topSides.Length; i++)
            {
                int topSide = topSides[i];
                int bottomSide = bottomSides[i];


                // Find the card by NetworkObjectId and use its existing reference
                NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectIds[i]];
                if (cardNetworkObject != null)
                {
                    Domino_data dominoComponent = cardNetworkObject.GetComponent<Domino_data>();

                    dominoComponent.InitializeDomino(topSide, bottomSide);
                    dominoComponent.AssignDominoVisual();

                    // Ensuring Card value and name is set correctly
                    cardNetworkObject.gameObject.name = $"Domino: [{topSide}-{bottomSide}]";
                    cardNetworkObject.TrySetParent(drawPileObj, false);

                    // Ensuring Card position is set correctly
                    cardNetworkObject.transform.localPosition = Vector3.zero;
                    cardNetworkObject.transform.localRotation = Quaternion.identity;


                    if(topSide == bottomSide)
                    {
                        dominoComponent.SetOpenHitboxes();
                    }
                }
            }

            Debug.Log("Client deck created.");
            yield return null;

            NotifyServerDeckCreationCompleteServerRpc();
        }


        [ServerRpc(RequireOwnership = false)]
        public void NotifyServerDeckCreationCompleteServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log("Client " + clientId + " has completed deck creation.");

            // Add client to the list of those who have completed deck creation
            clientsThatHaveCompletedDeck.Add(clientId);

            // If all clients have completed deck creation, proceed to start the game
            if (clientsThatHaveCompletedDeck.Count == miniManager.currentPlayerDictionary.Count)
            {
                Debug.Log("All clients have finished deck creation. Ready to start the game.");
            }
        }

        /// <summary>
        /// Shuffles deck on the server.
        /// </summary>
        public void ShuffleDeckServer()
        {
            if (IsServer)
            {
                Debug.Log("Shuffling deck on the server...");

                // Shuffle deck on the server
                for (int i = 0; i < deck.Count; i++)
                {
                    int randomIndex = UnityEngine.Random.Range(0, deck.Count);
                    var temp = deck[i];
                    deck[i] = deck[randomIndex];
                    deck[randomIndex] = temp;
                }

                Debug.Log("Deck shuffled.");
            }
        }

        /// <summary>
        /// Instatitates draw pile on the server.
        /// </summary>
        public void InstantiateDrawPileServer()
        {
            if (IsServer)
            {
                Debug.Log("Creating Draw Pile...");

                _drawPile.Clear(); // Clear previous draw pile if any

                // Copy shuffled deck into the draw pile
                foreach (var cardReference in deck)
                {
                    AddToDrawPileServer(cardReference);
                    SetCardActiveClientRpc(cardReference.NetworkObjectId, false);
                }

                Debug.Log("Draw Pile created.");
            }
        }



        [ClientRpc]
        void SetCardActiveClientRpc(ulong networkObjectId, bool value)
        {

            NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
            if (cardNetworkObject != null && cardNetworkObject.IsSpawned)
            {
                Debug.Log($"Server attempting to set {cardNetworkObject.gameObject.name} active to {value} on clients");
                cardNetworkObject.gameObject.SetActive(value);
                Debug.Log($"Checking if domino is active: {cardNetworkObject.isActiveAndEnabled}");
            }
            else
            {
                Debug.LogError("FATAL ERROR: domino not found on client.");
            }
        }

        [ClientRpc]
        void SetOpenHitboxesClientRpc(ulong networkObjectId)
        {

            NetworkObject dominoNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
            if (dominoNetworkObject != null && dominoNetworkObject.IsSpawned)
            {
                Debug.Log($"Server attempting to set {dominoNetworkObject.gameObject.name} open hitboxes on clients");
                dominoNetworkObject.GetComponent<Domino_data>().SetOpenHitboxes();
            }
            else
            {
                Debug.LogError("FATAL ERROR: domino not found on client.");
            }
        }

        [ClientRpc]
        void SetAllHitboxesOffClientRpc(ulong networkObjectId)
        {

            NetworkObject dominoNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
            if (dominoNetworkObject != null && dominoNetworkObject.IsSpawned)
            {
                Debug.Log($"Server attempting to set {dominoNetworkObject.gameObject.name} all hitboxes off on clients");
                dominoNetworkObject.GetComponent<Domino_data>().SetAllHitboxesOff();
            }
            else
            {
                Debug.LogError("FATAL ERROR: domino not found on client.");
            }
        }


        [ClientRpc]
        void SetSnapHitboxesClientRpc(ulong networkObjectId, bool isTopSide)
        {

            NetworkObject dominoNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
            if (dominoNetworkObject != null && dominoNetworkObject.IsSpawned)
            {
                Debug.Log($"Server attempting to set {dominoNetworkObject.gameObject.name} snapped open hitboxes on clients");
                dominoNetworkObject.GetComponent<Domino_data>().SetSnapHitboxes(isTopSide);
            }
            else
            {
                Debug.LogError("FATAL ERROR: domino not found on client.");
            }
        }

        private void AddToPlayPileServer(GameObject card)
        {
            if (IsServer)
            {
                NetworkObjectReference cardReference = new NetworkObjectReference(card.GetComponent<NetworkObject>());

                // Use the SetPlayed method to properly mark the domino as played and disable interaction
                var dominoData = card.GetComponent<Domino_data>();
                if (dominoData != null)
                {
                    dominoData.SetPlayed();
                    dominoData.inHand = false;
                }
                else
                {
                    Debug.LogError($"{card.name}: Domino_data component is missing. Cannot set as played.");
                }

                _playPile.Add(cardReference);
                AddToPileClientRpc(card.GetComponent<NetworkObject>().NetworkObjectId, true);
            }
        }
        private void AddToPlayPileServer(NetworkObjectReference cardReference)
        {
            if (IsServer)
            {
                if (cardReference.TryGet(out NetworkObject networkObject))
                {
                    _playPile.Add(cardReference);
                    AddToPileClientRpc(networkObject.NetworkObjectId, true);
                }
            }
        }

        private void AddToDrawPileServer(GameObject card)
        {
            if (IsServer)
            {
                NetworkObjectReference cardReference = new NetworkObjectReference(card.GetComponent<NetworkObject>());

                _drawPile.Add(cardReference);
                AddToPileClientRpc(card.GetComponent<NetworkObject>().NetworkObjectId, false);
            }
        }

        private void AddToDrawPileServer(NetworkObjectReference cardReference)
        {
            if (IsServer)
            {
                if (cardReference.TryGet(out NetworkObject networkObject))
                {
                    _drawPile.Add(cardReference);
                    AddToPileClientRpc(networkObject.NetworkObjectId, false);
                }
            }
        }

        /// <summary>
        /// Adds Card to selected pile for client(purely visual)
        /// </summary>
        /// <param name="networkObjectId"> the id of the card you are adding to the pile</param>
        /// <param name="isPlay">If you are adding to the play pile else draw pile</param>
        [ClientRpc]
        private void AddToPileClientRpc(ulong networkObjectId, bool isPlay)
        {
            StartCoroutine(AddToPileOnClient(networkObjectId, isPlay));
        }

        IEnumerator AddToPileOnClient(ulong networkObjectId, bool isPlay)
        {
            GameObject pileObj = isPlay ? playPileObj : drawPileObj;

            NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
            if (cardNetworkObject != null && cardNetworkObject.IsSpawned)
            {
                if (isPlay) // Set played to true to stop Play function from playing and disable XR grab interactable
                {
                    cardNetworkObject.gameObject.GetComponent<Domino_data>().played = true;
                }

                cardNetworkObject.gameObject.transform.parent = pileObj.transform;
                cardNetworkObject.gameObject.transform.localPosition = Vector3.zero;
                cardNetworkObject.gameObject.transform.localRotation = Quaternion.identity;
                cardNetworkObject.gameObject.GetComponent<Domino_data>().inHand = false;
            }


            yield return null;
        }


        private void RemoveGeneratedDominoesServer()
        {
            if (IsServer)
            {
                // Notify clients to clear their hands and card visuals
                ClearAllClientHandsClientRpc();

                foreach (NetworkedHandDomino hand in activeHands)
                {
                    hand.Clear();  // Clear the server-side hands
                    //hand.active = false;

                }

                activeHands.Clear();

                // Clear the piles
                _playPile.Clear();
                _drawPile.Clear();
                _playSides.Clear();


                foreach (NetworkObjectReference cardRef in deck)
                {
                    if (cardRef.TryGet(out NetworkObject networkCard) && networkCard.IsSpawned)
                    {
                        networkCard.gameObject.SetActive(true);
                        SetCardActiveClientRpc(networkCard.NetworkObjectId, true);
                        networkCard.Despawn(true); // Despawn the card across the network
                    }
                }
                deck.Clear();
            }
        }


        [ClientRpc]
        private void ClearAllClientHandsClientRpc()
        {
            drawObject.Clear();
            playObject.Clear();
            playSidesList.Clear();
            foreach (NetworkedHandDomino hand in activeHands)
            {
                hand.active = false;
            }
        }


        protected void StartDomino()
        {
            NetworkObjectReference firstReference = _drawPile[_drawPile.Count - 1];

            Debug.Log($"{activeHands[FindStartingPlayer()]} has the highest hands");
            currentHandIndex = FindStartingPlayer();

            if (firstReference.TryGet(out NetworkObject networkCardDraw))
            {
                _drawPile.Remove(firstReference);
                GameObject firstCard = networkCardDraw.gameObject;
                Debug.Log("Drawing First card(" + firstCard.name + ") for Domino . . . ");
                AddToPlayPileServer(firstCard);
                SetCardActiveClientRpc(firstReference.NetworkObjectId, true);
                SetOpenHitboxesClientRpc(firstReference.NetworkObjectId);

                _playSides.Add(networkCardDraw.GetComponent<Domino_data>().But_side);
                _playSides.Add(networkCardDraw.GetComponent<Domino_data>().Top_side);
            }
            else
            {
                Debug.Log("FATAL ERROR: Card not found at drawPiile(Domino)");
                return;
            }

            NetworkObjectReference cardReference = _drawPile[_drawPile.Count - 1];
            if (!cardReference.TryGet(out NetworkObject res))
            {
                Debug.Log("FATAL ERROR: Card not found at playPile(Domino)");
                return;
            }
            SetCardActiveClientRpc(cardReference.NetworkObjectId, true);

            //Debug.Log("First card drawn.");
            UpdateCurrentIndexClientRpc(currentHandIndex, 0);

            gameStarted = true;
        }


        private int FindStartingPlayer()
        {
            // Step: Find Player with Highest Domino, if that fails find player with highest single
            int indexDouble = -1;
            int indexSingle = -1;
            int highestDouble = -1;
            int highestSingle = -1;
            for(int i  = 0; i < activeHands.Count; i++)
            {
                foreach(var dominoRef in activeHands[i].heldDominos)
                {
                    NetworkObject domino = NetworkManager.Singleton.SpawnManager.SpawnedObjects[dominoRef.NetworkObjectId];
                    if(domino == null)
                    {
                        Debug.LogError("Domino didn't exist somehow?");
                        return -1;
                    }

                    if (domino.GetComponent<Domino_data>().But_side == domino.GetComponent<Domino_data>().Top_side)
                    {
                        if(domino.GetComponent<Domino_data>().But_side > highestDouble)
                        {
                            highestDouble = domino.GetComponent<Domino_data>().But_side;
                            indexDouble = i;
                            Debug.Log($"{activeHands[indexDouble].name} has new highest Double: {highestDouble}-{highestDouble}");

                        }
                    }
                    else
                    {
                        int singleTotal = domino.GetComponent<Domino_data>().But_side + domino.GetComponent<Domino_data>().Top_side;
                        if (singleTotal > highestSingle)
                        {
                            highestSingle = singleTotal;
                            indexSingle = i;
                            Debug.Log($"{activeHands[indexSingle].name} has new highest Single: {domino.GetComponent<Domino_data>().Top_side}-{domino.GetComponent<Domino_data>().But_side}");
                        }
                    }
                }
            }

            // Delete later but this debug stuff:
            if(indexDouble > -1)
            {
                Debug.Log($"There was a highest double, the double is {highestDouble} and the hand is {activeHands[indexDouble]}");
            }
            else
            {
                Debug.Log($"There is no highest double, the highest total single is {highestSingle} and the hand is {activeHands[indexSingle]}");
            }
            
            return (indexDouble > -1) ? indexDouble : indexSingle;
        }

        [ClientRpc]
        public void UpdatePlayerHandClientRpc(NetworkObjectReference cardReference, int index)
        {
            activeHands[index].DrawCardClientRpc(cardReference);  // Add card to the correct hand on the client side
        }

        public void RequestDrawDomino(GameObject card)
        {
            if (!_drawPile.Contains(card.GetComponent<NetworkObject>()))
            {
                Debug.Log($"Requesting to draw {card.name} not in pile");
                return;
            }

            NetworkObject networkObject = card.GetComponent<NetworkObject>();
            Debug.Log($"Client: {NetworkManager.Singleton.LocalClientId} is attempting to Draw {card.name}");

            // Check if it is said players turn to draw [Comment out if you want to play solo]
            //if (activeHands[currentHandIndex].ownerManager.ClientID != NetworkManager.Singleton.LocalClientId)
            //{
            //    Debug.Log($"It is not Client: {NetworkManager.Singleton.LocalClientId} turn!");
            //    string message = "It is not your turn to draw!";

            //    if (m_CurrentMessageRoutine != null)
            //    {
            //        StopCoroutine(m_CurrentMessageRoutine);
            //    }
            //    m_CurrentMessageRoutine = m_MiniGame.SendPlayerMessage(message, NetworkManager.Singleton.LocalClientId, 3);
            //    StartCoroutine(m_CurrentMessageRoutine);

            //    return;
            //}

            if (networkObject != null)
            {
                DrawTopDominoServerRpc(networkObject.NetworkObjectId);
            }
            else
            {
                Debug.Log("Error on request, card DNE");
            }

        }

        [ServerRpc(RequireOwnership = false)]
        private void DrawTopDominoServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
        {
            // To do Draw Dominoes from boneyard until you can play
            // To Do  Scoring: 
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"Server processing card draw request from client {clientId}.");

            if (IsServer)
            {

                NetworkObject domino = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
                if (domino != null)
                {
                    NetworkObjectReference dominoReference = new NetworkObjectReference(domino);
                    if (_drawPile.Contains(dominoReference))
                    {
                        _drawPile.Remove(dominoReference);  // Remove from draw pile
                        Debug.Log($"Card {domino.name} picked from draw pile.");

                        activeHands[currentHandIndex].DrawCardServerRpc(dominoReference); // This method is from NetworkedHand.cs

                        Debug.Log($"the current hand is: {currentHandIndex}. Server attempting to move {domino.name} to {activeHands[currentHandIndex].name} with id of {activeHands[currentHandIndex].NetworkObjectId}");

                        // Optionally trigger a client RPC to visually update clients on the draw action
                        UpdatePlayerHandClientRpc(dominoReference, currentHandIndex);


                        // ToDO: implement checking if card is valid don't skip turn
                        if (_playSides.Contains(domino.GetComponent<Domino_data>().But_side) || _playSides.Contains(domino.GetComponent<Domino_data>().Top_side))
                        {
                                UpdateCurrentIndexServerRpc();
                        }
                        else {
                            string message = domino.name + "cannot be played! Keep drawing lmao";

                            if (m_CurrentMessageRoutine != null)
                            {
                                StopCoroutine(m_CurrentMessageRoutine);
                            }
                            m_CurrentMessageRoutine = m_MiniGame.SendPlayerMessage(message, clientId, 3);
                            StartCoroutine(m_CurrentMessageRoutine);
                            Debug.Log("Domino cannot be played keep on drawing lmao");
                        }

                        if (_drawPile.Count > 0)
                        {
                            if (_drawPile[_drawPile.Count - 1].TryGet(out NetworkObject nextDomino))
                            {
                                SetCardActiveClientRpc(nextDomino.NetworkObjectId, true);
                            }
                        }

                    }
                    else
                    {
                        Debug.Log("Card not found in draw pile.");
                    }

                }
            }
        }

        public void RequestPlayDomino(ulong networkObjectIdsnap, ulong networkObjectIdstill, int hitbox, bool isTopSide)
        {
            NetworkObject dominoSnap = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectIdsnap];
            NetworkObject dominoStill = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectIdstill];

            if (dominoSnap.IsSpawned && dominoStill.IsSpawned)
            {
                Debug.Log($"Network Object({dominoSnap.name}, {dominoStill.name}) is Spawned and ready for use.");
            }
            else
            {
                Debug.Log($"Fatal Error! Network Object({dominoSnap.name}{dominoStill.name}) is not spawned");
                return;
            }

            Debug.Log($"Client: {NetworkManager.Singleton.LocalClientId} is attempting to play {dominoSnap.name}");
            if (!activeHands[currentHandIndex].heldDominos.Contains(dominoSnap)) // Card from wrong hand do not accept
            {
                Debug.Log($"It is not Client: {NetworkManager.Singleton.LocalClientId}'s turn!");
                return;
            }

            if (dominoSnap != null && dominoStill != null)
            {
                PlayDominoServerRpc(networkObjectIdsnap, networkObjectIdstill, hitbox, isTopSide);
            }
            else
            {
                Debug.Log("Error on request, card DNE");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayDominoServerRpc(ulong networkObjectIdsnap, ulong networkObjectIdstill, int hitbox, bool isTopSide, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"Server processing card play request from client {clientId}.");

            if (IsServer)
            {
                NetworkObject dominoSnap = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectIdsnap];
                NetworkObject dominoStill = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectIdstill];
                if (dominoSnap != null && dominoStill != null)
                {
                    dominoSnap.GetComponent<Domino_data>().played = true;
                    NetworkObjectReference dominoReference = new NetworkObjectReference(dominoSnap);

                    activeHands[currentHandIndex].RemoveCardServerRpc(dominoReference.NetworkObjectId);

                    _playSides.Remove(dominoStill.GetComponent<SnapManager>().hitboxes[hitbox].sideValue);
                    _playSides.Add(isTopSide ? dominoSnap.GetComponent<Domino_data>().But_side : dominoSnap.GetComponent<Domino_data>().Top_side);


                    AddToPlayPileServer(dominoReference);
                    PlayDominoClientRpc(dominoReference.NetworkObjectId, networkObjectIdstill, hitbox, isTopSide);

                    UpdateCurrentIndexServerRpc();
                }
            }
        }

        [ClientRpc]
        public void PlayDominoClientRpc(ulong networkObjectIdsnap, ulong networkObjectIdstill, int hitbox, bool isTopSide)
        {
            NetworkObject dominoSnap = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectIdsnap];
            NetworkObject dominoStill = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectIdstill];


            if (dominoSnap != null && dominoStill != null)
            {
                Debug.Log("Is this the topside?: " + isTopSide);

                Transform hitboxTransform = dominoStill.GetComponent<SnapManager>().hitboxes[hitbox].transform;
                dominoSnap.transform.position = hitboxTransform.position;


                Vector3 baseRotation = hitboxTransform.rotation.eulerAngles;
                baseRotation.z = isTopSide ? baseRotation.z - 180 : baseRotation.z;
                dominoSnap.transform.rotation = Quaternion.Euler(baseRotation);

                if (dominoSnap.GetComponent<Domino_data>().But_side == dominoSnap.GetComponent<Domino_data>().Top_side)
                {
                    baseRotation.z = baseRotation.z - 90;
                    dominoSnap.transform.rotation = Quaternion.Euler(baseRotation);
                }
                else
                {
                    switch (hitbox)
                    {
                        case 0: // Top & But Domino
                            dominoSnap.transform.position += dominoStill.transform.up * 0.0306988f;
                            break;
                        case 1: // But Domino
                            dominoSnap.transform.position -= dominoStill.transform.up * 0.0306988f;
                            break;
                        case 2: // Left Domino 
                        case 4:
                            dominoSnap.transform.position += dominoStill.transform.right * 0.03450492f;
                            break;
                        case 3: // Right Domino 
                        case 5:
                            dominoSnap.transform.position -= dominoStill.transform.right * 0.03450492f;
                            break;
                    }
                }

                SetOpenHitboxesClientRpc(dominoStill.NetworkObjectId);
                SetSnapHitboxesClientRpc(dominoSnap.NetworkObjectId, isTopSide);


                // Mark this hitbox as used
                dominoStill.GetComponent<SnapManager>().hitboxes[hitbox].GetComponent<HitboxComponent>().isUsed = true;
                dominoSnap.GetComponent<XRGrabInteractable>().enabled = false;



                // Reset the color to transparent after snapping
                dominoStill.GetComponent<SnapManager>().hitboxes[hitbox].GetComponent<HitboxComponent>().SetColor(new Color(1, 1, 1, 0));
            }
        }



        public void RequestPlayFirstDomino(ulong networkObjectId)
        {
            NetworkObject networkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];

            if (networkObject.IsSpawned)
            {
                Debug.Log($"Network Object({networkObject.name}) is Spawned and ready for use.");
            }
            else
            {
                Debug.Log($"Fatal Error! Network Object({networkObject.name}) is not spawned");
                return;
            }

            Debug.Log($"Client: {NetworkManager.Singleton.LocalClientId} is attempting to play {networkObject.name}");
            if (!activeHands[currentHandIndex].heldDominos.Contains(networkObject)) // Card from wrong hand do not accept
            {
                Debug.Log($"It is not Client: {NetworkManager.Singleton.LocalClientId}'s turn!");
                return;
            }

            if (networkObject != null)
            {
                PlayFirstDominoServerRpc(networkObjectId);
            }
            else
            {
                Debug.Log("Error on request, card DNE");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayFirstDominoServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"Server processing first domino play request from client {clientId}.");

            if (IsServer)
            {
                NetworkObject dominoNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
                if (dominoNetworkObject != null)
                {

                    //if (!IsValidPlayCrazyEights(cardNetworkObject.gameObject))
                    //{
                    //    string message = cardNetworkObject.gameObject.name + "is not a valid play!";

                    //    if (m_CurrentMessageRoutine != null)
                    //    {
                    //        StopCoroutine(m_CurrentMessageRoutine);
                    //    }
                    //    m_CurrentMessageRoutine = m_MiniGame.SendPlayerMessage(message, clientId, 3);
                    //    StartCoroutine(m_CurrentMessageRoutine);
                    //    return;
                    //}

                    dominoNetworkObject.GetComponent<Domino_data>().played = true;
                    NetworkObjectReference dominoReference = new NetworkObjectReference(dominoNetworkObject);

                    activeHands[currentHandIndex].RemoveCardServerRpc(dominoReference.NetworkObjectId);


                    dominoNetworkObject.GetComponent<Domino_data>().isFirstDomino = true;

                    Debug.Log("Adding to play pile");
                    AddToPlayPileServer(dominoNetworkObject);
                    SetOpenHitboxesClientRpc(dominoNetworkObject.NetworkObjectId);
                    playPileObj.GetComponent<PlayPileDomino>().firstDominoPlayed = true;


     

                    UpdateCurrentIndexServerRpc();
                }
            }
        }

        [ServerRpc]
        public void UpdateCurrentIndexServerRpc()
        {
            if (IsServer)
            {
                int prevHandIndex = currentHandIndex;
                if (currentHandIndex == activeHands.Count - 1) { currentHandIndex = 0; }
                else { currentHandIndex++; }

                UpdateCurrentIndexClientRpc(currentHandIndex, prevHandIndex);
            }
            else
            {
                Debug.Log("FATAL ERROR: IDK WHAT HAPPENED HERE BY THE UPDATE FUNC IS SCREWED");
            }

        }

        [ClientRpc]
        private void UpdateCurrentIndexClientRpc(int newIndex, int oldIndex)
        {
            if (oldIndex >= 0 && oldIndex < activeHands.Count && activeHands[oldIndex].ownerManager.ClientID == NetworkManager.Singleton.LocalClientId && gameStarted)
            {
                Debug.Log($"Ending turn for hand owner with ID: {activeHands[oldIndex].ownerManager.ClientID}");

                if (m_CurrentMessageRoutine != null)
                {
                    StopCoroutine(m_CurrentMessageRoutine);
                }
                m_CurrentMessageRoutine = m_MiniGame.SendPlayerMessage("Your turn has ended!", NetworkManager.Singleton.LocalClientId, 3);
                StartCoroutine(m_CurrentMessageRoutine);
            }

            // Update the current hand index only if it's within bounds
            Debug.Log($"{newIndex} {oldIndex} {activeHands.Count}");
            if (newIndex >= 0 && newIndex < activeHands.Count)
            {
                currentHandIndex = newIndex;
                Debug.Log($"New hand owner ID: {activeHands[currentHandIndex].ownerManager.ClientID}");

                if (activeHands[currentHandIndex].ownerManager.ClientID == NetworkManager.Singleton.LocalClientId)
                {
                    if (m_CurrentMessageRoutine != null)
                    {
                        StopCoroutine(m_CurrentMessageRoutine);
                    }
                    m_CurrentMessageRoutine = m_MiniGame.SendPlayerMessage("It's your turn!", NetworkManager.Singleton.LocalClientId, 3);
                    StartCoroutine(m_CurrentMessageRoutine);
                }
            }
            else
            {
                Debug.LogError("New index is out of bounds for activeHands.");
            }

            Debug.Log("Server has set current hand to " + (currentHandIndex < activeHands.Count ? activeHands[currentHandIndex].name : "Invalid Index"));
        }
        public void CheckForPlayerLeave()
        {
            if (gameStarted)
            {
                if (miniManager.currentPlayerDictionary.Count < activeHands.Count)
                {
                    if (m_CurrentMessageRoutine != null)
                    {
                        StopCoroutine(m_CurrentMessageRoutine);
                    }
                    StartCoroutine(m_MiniGame.PlayerLeftRoutine());
                }
            }
        }

        public void CheckForPlayerWin()
        {
            if (gameStarted)
            {
                foreach (NetworkedHandDomino hand in activeHands)
                {
                    if (hand.isEmpty())
                    {
                        Debug.Log(hand.name + "is empty, calling courotine");
                        if (m_CurrentMessageRoutine != null)
                        {
                            StopCoroutine(m_CurrentMessageRoutine);
                        }

                        StartCoroutine(m_MiniGame.PlayerWonRoutine(hand));
                    }
                }
            }
        }

        private void OnDeckChanged(NetworkListEvent<NetworkObjectReference> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<NetworkObjectReference>.EventType.Add:
                    // A new card was added to the draw pile
                    Debug.Log($"Card added to Deck: {changeEvent.Value}");
                    break;

                case NetworkListEvent<NetworkObjectReference>.EventType.Remove:
                    // A card was removed from the draw pile
                    Debug.Log($"Card removed from Deck: {changeEvent.Value}");
                    break;
            }
        }
        private void OnDrawPileChanged(NetworkListEvent<NetworkObjectReference> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<NetworkObjectReference>.EventType.Add:
                    // A new card was added to the draw pile
                    Debug.Log($"Card added to draw pile: {changeEvent.Value}");
                    if (changeEvent.Value.TryGet(out NetworkObject noA))
                    {
                        drawObject.Add(noA.gameObject);
                    }
                    break;

                case NetworkListEvent<NetworkObjectReference>.EventType.Remove:
                    // A card was removed from the draw pile
                    Debug.Log($"Card removed from draw pile: {changeEvent.Value}");
                    if (changeEvent.Value.TryGet(out NetworkObject noR))
                    {
                        drawObject.Remove(noR.gameObject);
                    }
                    break;
            }
        }
        private void OnPlayPileChanged(NetworkListEvent<NetworkObjectReference> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<NetworkObjectReference>.EventType.Add:
                    // A new card was added to the draw pile
                    Debug.Log($"Card added to play pile: {changeEvent.Value}");
                    if (changeEvent.Value.TryGet(out NetworkObject noA))
                    {
                        playObject.Add(noA.gameObject);
                    }
                    break;

                case NetworkListEvent<NetworkObjectReference>.EventType.Remove:
                    // A card was removed from the draw pile
                    Debug.Log($"Card removed from play pile: {changeEvent.Value}");
                    if (changeEvent.Value.TryGet(out NetworkObject noR))
                    {
                        playObject.Remove(noR.gameObject);
                    }
                    break;
            }
        }

        private void OnPlayableSidesChanged(NetworkListEvent<int> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<int>.EventType.Add:
                    // A new card was added to the draw pile
                    Debug.Log($"Play Side added to List: {changeEvent.Value}");
                    playSidesList.Add(changeEvent.Value);
                    break;

                case NetworkListEvent<int>.EventType.Remove:
                    // A card was removed from the draw pile
                    Debug.Log($"Play Side removed from List: {changeEvent.Value}");
                    playSidesList.Remove(changeEvent.Value);
                    break;
            }
        }


    }
}

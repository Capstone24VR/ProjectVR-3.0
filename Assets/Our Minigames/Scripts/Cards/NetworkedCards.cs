using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Card;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEditor.PackageManager;
using static XRMultiplayer.MiniGames.MiniGameManager;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents a networked version of the Whack-A-Pig mini game.
    /// </summary>
    public class NetworkedCards : NetworkBehaviour
    {
        /// <summary>
        /// The hands to use for playing.
        /// </summary>
        [SerializeField] NetworkedHand[] m_hands;

        /// <summary>
        /// The card prefab to spawn.
        /// </summary>
        [SerializeField] GameObject card;

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
        MiniGame_Cards m_MiniGame;

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


        [SerializeField] protected NetworkList<NetworkObjectReference> deck = new NetworkList<NetworkObjectReference>();
        [SerializeField] protected List<GameObject> drawObject = new List<GameObject>();
        [SerializeField] protected List<GameObject> playObject = new List<GameObject>();

        [SerializeField] protected NetworkList<NetworkObjectReference> _drawPile = new NetworkList<NetworkObjectReference>();
        [SerializeField] protected NetworkList<NetworkObjectReference> _playPile = new NetworkList<NetworkObjectReference>();

        [SerializeField] protected int currentHandIndex;

        [SerializeField] protected List<int> activeIndex = new List<int>();
        [SerializeField] protected List<NetworkedHand> activeHands = new List<NetworkedHand>();

        [SerializeField] private MiniGameManager miniManager;

        void Start()
        {
            TryGetComponent(out m_MiniGame);
            deck.OnListChanged += OnDeckChanged;
            _drawPile.OnListChanged += OnDrawPileChanged;
            _playPile.OnListChanged += OnPlayPileChanged;

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
            StopAllCoroutines();
            RemoveGeneratedCardsServer();

            yield return new WaitForSeconds(0.5f); // Give time for clients to catch u

            if (IsServer)
            {
                StartCoroutine(WaitForClientConnection());
            }
        }

        public void StartGame()
        {
            //GetActiveHandsServer();
            activeHands.Add(m_hands[0]);
            activeHands.Add(m_hands[1]);
            CreateDeckServer();
            ShuffleDeckServer();
            InstantiateDrawPileServer();

            StartCoroutine(WaitForClientsToCreateDeck());
        }

        public void EndGame()
        {
            StopAllCoroutines();
            RemoveGeneratedCardsServer();
        }


        private void GetActiveHandsServer()
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

        [ClientRpc]
        private void SyncActiveHandsClientRpc(int[] index)
        {
            List<NetworkedHand> duplicate = new List<NetworkedHand>(index.Length);
            for (int i = 0; i < index.Length; i++)
            {
                duplicate.Add(m_hands[index[i]]);
            }

            activeHands = duplicate;
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
                foreach (NetworkedHand hand in activeHands)
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
            StartCrazyEights();
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
            Debug.Log("Creating Deck ServerSide . . .");

            List<int> suits = new List<int>();
            List<int> values = new List<int>();
            List<ulong> networkObjectIds = new List<ulong>();

            int cardsSpawned = 0;

            // Iterate through suits and values to create a full deck
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Value value in Enum.GetValues(typeof(Value)))
                {

                    // Create the card GameObject and get its NetworkObject component
                    GameObject newCard = Instantiate(card, drawPileObj.transform, false); // Create card
                    var networkObject = newCard.GetComponent<NetworkObject>(); // Get NetworkObject for server spawning

                    if (networkObject != null)
                    {
                        networkObject.Spawn();
                        Debug.Log($"{suit} {value} has been spawned with id of: {networkObject.NetworkObjectId}");
                        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(networkObject.NetworkObjectId))
                        {
                            Debug.LogError($"Failed to register {suit} {value} with NetworkObjectId {networkObject.NetworkObjectId}");
                        }
                        cardsSpawned++;
                    }
                    else
                    {
                        Debug.LogError("NetworkObject component is missing on the card prefab.");
                        continue; // Skip to the next iteration if no NetworkObject is found
                    }

                    // Ensure Card component exists before assigning values
                    Card cardComponent = newCard.GetComponent<Card>();
                    if (cardComponent != null)
                    {
                        cardComponent.suit = suit;
                        cardComponent.value = value;
                        newCard.name = $"Card: {suit} {value}";

                        // Add card data to lists for client notification
                        suits.Add((int)suit);
                        values.Add((int)value);
                        networkObjectIds.Add(networkObject.NetworkObjectId);

                        // Add card to deck (NetworkList)
                        deck.Add(new NetworkObjectReference(networkObject));

                    }
                    else
                    {
                        Debug.LogError("Card component is missing on the new card prefab.");
                    }
                }
            }


            while (cardsSpawned < 52)
            {
                yield return null;  // Wait until next frame;
            }

            Debug.Log("Deck created ServerSide. Notifying Clients . . .");
            // Notify clients with card data
            CreateDeckClientRpc(suits.ToArray(), values.ToArray(), networkObjectIds.ToArray());
        }


        // ClientRpc method with simple data types instead of custom struct
        [ClientRpc]
        public void CreateDeckClientRpc(int[] suits, int[] values, ulong[] networkObjectIds)
        {
            StartCoroutine(CreateDeckOnClient(suits, values, networkObjectIds));
        }

        IEnumerator CreateDeckOnClient(int[] suits, int[] values, ulong[] networkObjectIds)
        {
            Debug.Log("Client received deck creation. Waiting for server to finish...");

            yield return new WaitForSeconds(1.0f); // Ensure enough delay for server to fully spawn cards

            Debug.Log("Creating Deck on Client...");

            for (int i = 0; i < suits.Length; i++)
            {
                Suit suit = (Suit)suits[i];
                Value value = (Value)values[i];

                // Load the appropriate prefab based on suit and value
                UnityEngine.Object pPrefab = ((int)value > 1 && (int)value < 11) ?
                    Resources.Load("Free_Playing_Cards/PlayingCards_" + (int)value + suit) :
                    Resources.Load("Free_Playing_Cards/PlayingCards_" + value + suit);

                if (pPrefab == null)
                {
                    Debug.LogError("(Client)Prefab not found: " + "Free_Playing_Cards/PlayingCards_" + value + suit);
                    continue;
                }

                // Find the card by NetworkObjectId and use its existing reference
                NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectIds[i]];
                if (cardNetworkObject != null)
                {
                    // Ensuring Card value and name is set correctly
                    cardNetworkObject.gameObject.name = $"Card: {suit} {value}";
                    cardNetworkObject.gameObject.GetComponent<Card>().suit = suit;
                    cardNetworkObject.gameObject.GetComponent<Card>().value = value;
                    cardNetworkObject.TrySetParent(drawPileObj, false);

                    // Ensuring Card position is set correctly
                    cardNetworkObject.transform.localPosition = Vector3.zero;
                    cardNetworkObject.transform.localRotation = Quaternion.identity;

                    // Instantiate model on the client
                    GameObject model = (GameObject)Instantiate(pPrefab, cardNetworkObject.transform, false);

                    if (model == null)
                    {
                        Debug.LogError("Model instantiation failed.");
                        continue;
                    }

                    // FOR DEBUG PURPOSES ONLY
                    model.name = model.name + "(CLIENT)";
                    //

                    model.transform.localRotation = Quaternion.identity;
                    model.transform.localPosition = Vector3.zero;
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
                Debug.Log($"Checking if card is active: {cardNetworkObject.isActiveAndEnabled}");
            }
            else
            {
                Debug.LogError("FATAL ERROR: Card not found on client.");
            }
        }

        private void AddToPlayPileServer(GameObject card)
        {
            if (IsServer)
            {
                NetworkObjectReference cardReference = new NetworkObjectReference(card.GetComponent<NetworkObject>());
                card.GetComponent<Card>().played = true;
                card.GetComponent<Card>().inHand = false;

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
            SetCardActiveClientRpc(networkObjectId, false);
        }

        IEnumerator AddToPileOnClient(ulong networkObjectId, bool isPlay)
        {
            GameObject pileObj = isPlay ? playPileObj : drawPileObj;

            NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
            if (cardNetworkObject != null && cardNetworkObject.IsSpawned)
            {
                if (isPlay) // Set played to true to stop Play function from playing and disable XR grab interactable
                {
                    cardNetworkObject.gameObject.GetComponent<Card>().played = true;
                }

                cardNetworkObject.gameObject.transform.parent = pileObj.transform;
                cardNetworkObject.gameObject.transform.localPosition = Vector3.zero;
                cardNetworkObject.gameObject.transform.localRotation = Quaternion.identity;
                cardNetworkObject.gameObject.GetComponent<Card>().inHand = false;
            }
            else
            {
                Debug.Log($"Catastrophic Error: Could not add {card.name} to pile");
            }

            yield return null;
        }


        private void RemoveGeneratedCardsServer()
        {
            if (IsServer)
            {
                // Notify clients to clear their hands and card visuals
                ClearAllClientHandsClientRpc();

                foreach (NetworkedHand hand in activeHands)
                {
                    hand.Clear();  // Clear the server-side hands
                }

                activeHands.Clear();

                // Clear the piles
                _playPile.Clear();
                _drawPile.Clear();

                foreach (NetworkObjectReference cardRef in deck)
                {
                    if (cardRef.TryGet(out NetworkObject networkCard) && networkCard.IsSpawned)
                    {
                        networkCard.gameObject.SetActive(true);
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
            foreach (NetworkedHand hand in activeHands)
            {
                hand.Clear();  // Clients also clear their hands
            }
        }


        protected void StartCrazyEights()
        {
            NetworkObjectReference firstReference = _drawPile[_drawPile.Count - 1];
            if (firstReference.TryGet(out NetworkObject networkCardDraw))
            {
                _drawPile.Remove(firstReference);
                GameObject firstCard = networkCardDraw.gameObject;
                Debug.Log("Drawing First card(" + firstCard.name + ") for Crazy Eights . . . ");
                AddToPlayPileServer(firstCard);
                SetCardActiveClientRpc(firstReference.NetworkObjectId, true);
            }
            else
            {
                Debug.Log("FATAL ERROR: Card not found at drawPiile(STARTCRZY8s)");
                return;
            }

            NetworkObjectReference cardReference = _drawPile[_drawPile.Count - 1];
            if (!cardReference.TryGet(out NetworkObject res))
            {
                Debug.Log("FATAL ERROR: Card not found at playPile(STARTCRZY8s)");
                return;
            }
            SetCardActiveClientRpc(cardReference.NetworkObjectId, true);

            Debug.Log("First card drawn.");
            UpdateCurrentIndexClientRpc(currentHandIndex, 0);

            gameStarted = true;
        }

        [ClientRpc]
        public void UpdatePlayerHandClientRpc(NetworkObjectReference cardReference, int index)
        {
            activeHands[index].DrawCardClientRpc(cardReference);  // Add card to the correct hand on the client side
        }

        public void RequestDrawCard(GameObject card)
        {
            if (!_drawPile.Contains(card.GetComponent<NetworkObject>())) {
                Debug.Log($"Requesting to draw {card.name} not in pile");
                return; 
            }

            NetworkObject networkObject = card.GetComponent<NetworkObject>();
            Debug.Log($"Client: {NetworkManager.Singleton.LocalClientId} is attempting to Draw {card.name}");

            // Check if it is said players turn to draw [Comment out if you want to play solo]
            if (activeHands[currentHandIndex].ownerManager.ClientID != NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log($"It is not Client: {NetworkManager.Singleton.LocalClientId} turn!");
                string message = "It is not your turn to draw!";

                if (m_CurrentMessageRoutine != null)
                {
                    StopCoroutine(m_CurrentMessageRoutine);
                }
                m_CurrentMessageRoutine = m_MiniGame.SendPlayerMessage(message, NetworkManager.Singleton.LocalClientId, 3);
                StartCoroutine(m_CurrentMessageRoutine);

                return;
            }

            if (networkObject != null)
            {
                DrawTopCardServerRpc(networkObject.NetworkObjectId);
            }
            else
            {
                Debug.Log("Error on request, card DNE");
            }

        }

        [ServerRpc(RequireOwnership = false)]
        private void DrawTopCardServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"Server processing card draw request from client {clientId}.");

            if (IsServer)
            {

                NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
                if (cardNetworkObject != null)
                {
                    NetworkObjectReference cardReference = new NetworkObjectReference(cardNetworkObject);
                    if (_drawPile.Contains(cardReference))
                    {
                        _drawPile.Remove(cardReference);  // Remove from draw pile
                        Debug.Log($"Card {cardNetworkObject.gameObject.name} picked from draw pile.");

                        activeHands[currentHandIndex].DrawCardServerRpc(cardReference); // This method is from NetworkedHand.cs

                        Debug.Log($"the current hand is: {currentHandIndex}. Server attempting to move {cardNetworkObject.gameObject.name} to {activeHands[currentHandIndex].name} with id of {activeHands[currentHandIndex].NetworkObjectId}");

                        // Optionally trigger a client RPC to visually update clients on the draw action
                        UpdatePlayerHandClientRpc(cardReference, currentHandIndex);


                        // ToDO: implement checking if card is valid don't skip turn
                        if (IsValidPlayCrazyEights(cardNetworkObject.gameObject))
                        {
                            Debug.Log("Card is active and can be played!");
                        }

                        else
                        {
                            UpdateCurrentIndexServerRpc();
                        }

                        if (_drawPile.Count > 0)
                        {
                            if (_drawPile[_drawPile.Count - 1].TryGet(out NetworkObject nextCard))
                            {
                                SetCardActiveClientRpc(nextCard.NetworkObjectId, true);
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

        public void RequestPlayCard(ulong networkObjectId)
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

            Debug.Log($"Client: {NetworkManager.Singleton.LocalClientId} is attempting to play {networkObject.gameObject.name}");
            if (!activeHands[currentHandIndex].heldCards.Contains(networkObject)) // Card from wrong hand do not accept
            {
                Debug.Log($"It is not Client: {NetworkManager.Singleton.LocalClientId}'s turn!");
                return;
            }

            if (networkObject != null)
            {
                PlayCardServerRpc(networkObjectId);
            }
            else
            {
                Debug.Log("Error on request, card DNE");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayCardServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"Server processing card play request from client {clientId}.");

            if (IsServer)
            {
                NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
                if (cardNetworkObject != null)
                {

                    if (!IsValidPlayCrazyEights(cardNetworkObject.gameObject))
                    {
                        string message = cardNetworkObject.gameObject.name + "is not a valid play!";

                        if (m_CurrentMessageRoutine != null)
                        {
                            StopCoroutine(m_CurrentMessageRoutine);
                        }
                        m_CurrentMessageRoutine = m_MiniGame.SendPlayerMessage(message, clientId, 3);
                        StartCoroutine(m_CurrentMessageRoutine);
                        return;
                    }


                    cardNetworkObject.gameObject.GetComponent<Card>().played = true;
                    NetworkObjectReference cardReference = new NetworkObjectReference(cardNetworkObject);

                    activeHands[currentHandIndex].RemoveCardServerRpc(cardReference.NetworkObjectId);

                    if (_playPile.Count > 0) // Get Old Top Card Reference
                    {
                        NetworkObjectReference oldTopCardReference = _playPile[_playPile.Count - 1];
                        AddToPlayPileServer(cardReference);
                        PlayCardClientRpc(cardReference.NetworkObjectId);

                        Debug.Log($"Checking {cardNetworkObject.gameObject.name} status. . . played: {cardNetworkObject.gameObject.GetComponent<Card>().played}\t parent: {cardNetworkObject.gameObject.transform.parent}\t position:{cardNetworkObject.gameObject.transform.localRotation}\t position:{cardNetworkObject.gameObject.transform.localRotation}");

                        SetCardActiveClientRpc(cardReference.NetworkObjectId, true);
                        SetCardActiveClientRpc(oldTopCardReference.NetworkObjectId, false);

                        UpdateCurrentIndexServerRpc();
                    }
                }
            }
        }

        [ClientRpc]
        public void PlayCardClientRpc(ulong networkObjectId)
        {
            NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
            if (cardNetworkObject != null)
            {
                cardNetworkObject.GetComponent<Card>().SetPosition(Vector3.zero);
                cardNetworkObject.GetComponent<Card>().ResetPosition();
            }
        }

        protected bool IsValidPlayCrazyEights(GameObject card)
        {
            if (_playPile.Count > 0)
            {
                NetworkObjectReference cardReference = _playPile[_playPile.Count - 1];
                if (cardReference.TryGet(out NetworkObject networkCard))
                {
                    Card topCard = networkCard.gameObject.GetComponent<Card>();
                    if (topCard.suit == card.GetComponent<Card>().suit)
                    {
                        Debug.Log("Cards share the same suit: " + card.GetComponent<Card>().suit);
                        return true;
                    }
                    else if (topCard.value == card.GetComponent<Card>().value)
                    {
                        Debug.Log("Cards share the same value: " + card.GetComponent<Card>().value);
                        return true;
                    }
                }
                else
                {
                    Debug.Log("FATAL ERROR: Card not found at playPile(ISVALIDPLAY)");
                }
            }
            return false;
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
            Debug.Log($"Current Hand owner id: {activeHands[oldIndex].ownerManager.ClientID}  \tClientid:  {NetworkManager.Singleton.LocalClientId}  \tGame Started: {gameStarted}");
            if (activeHands[oldIndex].ownerManager.ClientID == NetworkManager.Singleton.LocalClientId && gameStarted)
            {
                if (m_CurrentMessageRoutine != null)
                {
                    StopCoroutine(m_CurrentMessageRoutine);
                }
                m_CurrentMessageRoutine = m_MiniGame.SendPlayerMessage("Your turn has ended!", NetworkManager.Singleton.LocalClientId, 3);
                StartCoroutine(m_CurrentMessageRoutine);
            }

            currentHandIndex = newIndex;


            Debug.Log($"New Hand owner id: {activeHands[currentHandIndex].ownerManager.ClientID}  \tClientid:  {NetworkManager.Singleton.LocalClientId}  \tGame Started: {gameStarted}");

            if (activeHands[currentHandIndex].ownerManager.ClientID == NetworkManager.Singleton.LocalClientId)
            {
                if (m_CurrentMessageRoutine != null)
                {
                    StopCoroutine(m_CurrentMessageRoutine);
                }
                m_CurrentMessageRoutine = m_MiniGame.SendPlayerMessage("Its your turn!", NetworkManager.Singleton.LocalClientId, 3);
                StartCoroutine(m_CurrentMessageRoutine);
            }

            Debug.Log("Server has set current hand to " + activeHands[currentHandIndex].name);
        }

        public void CheckForPlayerWin()
        {
            if (gameStarted)
            {
                foreach (NetworkedHand hand in activeHands)
                {
                    if (hand.isEmpty())
                    {
                        Debug.Log(hand.name + "is empty, calling courotine");
                        if(m_CurrentMessageRoutine != null)
                        {
                            StopCoroutine (m_CurrentMessageRoutine);
                        }

                        StartCoroutine(m_MiniGame.PlayerWonRoutine(hand.gameObject));
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

        public override void OnDestroy()
        {
            foreach(var hand in m_hands)
            {
                hand.ownerManager.seatHandler.OnTriggerAction -= TriggerReadyState;
            }
        }
    }
}

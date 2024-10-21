using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static Card;
using Unity.Netcode;

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
        /// The current routine being played.
        /// </summary>
        IEnumerator m_CurrentRoutine;

        /// <summary>
        /// Keeps track of the clients who have completed deck creation
        /// </summary>
        private HashSet<ulong> clientsThatHaveCompletedDeck = new HashSet<ulong>();

        /// <summary>
        /// The number of starting cards
        /// </summary>
        [SerializeField] int startingHand = 5;


        [SerializeField] protected NetworkList<NetworkObjectReference> deck = new NetworkList<NetworkObjectReference>();
        [SerializeField] protected List<GameObject> deckObject = new List<GameObject>();

        [SerializeField] protected NetworkList<NetworkObjectReference> _drawPile = new NetworkList<NetworkObjectReference>();
        [SerializeField] protected NetworkList<NetworkObjectReference> _playPile = new NetworkList<NetworkObjectReference>();

        [SerializeField] protected int currentHandIndex;

        [SerializeField] protected List<NetworkedHand> activeHands = new List<NetworkedHand>();

        [SerializeField] private MiniGameManager miniManager;

        void Start()
        {
            TryGetComponent(out m_MiniGame);
            deck.OnListChanged += OnDeckChanged;
            _drawPile.OnListChanged += OnDrawPileChanged;
            _playPile.OnListChanged += OnPlayPileChanged;
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
            CreateDeckServer();
            ShuffleDeckServer();
            InstantiateDrawPileServer();

            StartCoroutine(WaitForClientsToCreateDeck());
        }

        private IEnumerator WaitForClientsToCreateDeck()
        {
            Debug.Log("Waiting for client's to finish deck creation . . . ");

            while (clientsThatHaveCompletedDeck.Count < miniManager.currentPlayerDictionary.Count)
            {
                yield return null;
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

        public void EndGame()
        {
            StopAllCoroutines();
            RemoveGeneratedCardsServer();
        }

        public void UpdateGame(float deltaTime)
        {
            CheckForPlayerWin();
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
                        newCard.SetActive(true); // Hide the card until needed

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

            Debug.Log("Deck created ServerSide. Notifying Clients . . .");
            yield return new WaitForSeconds(0.5f); // Optional delay for synchronization

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
                // Optionally, you can directly start the game here if desired
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
                Debug.Log($"Start Crazy Eights, attempting to set card{cardNetworkObject.gameObject.name} Active on clients");
                cardNetworkObject.gameObject.SetActive(value);
                Debug.Log($"Checking if card is active: " + cardNetworkObject.isActiveAndEnabled);
            }
            else
            {
                Debug.LogError("FATAL ERROR: Card not found on client.");
            }
        }

        private void AddToPlayPileServer(GameObject card)
        {
            NetworkObjectReference cardReference = new NetworkObjectReference(card.GetComponent<NetworkObject>());

            _playPile.Add(cardReference);
            AddToPileClientRpc(card.GetComponent<NetworkObject>().NetworkObjectId, true);
        }

        private void AddToPlayPileServer(NetworkObjectReference cardReference)
        {
            if (cardReference.TryGet(out NetworkObject networkObject))
            {
                _playPile.Add(cardReference);
                AddToPileClientRpc(networkObject.NetworkObjectId, true);
            }
        }

        private void AddToDrawPileServer(GameObject card)
        {
            NetworkObjectReference cardReference = new NetworkObjectReference(card.GetComponent<NetworkObject>());

            _drawPile.Add(cardReference);
            AddToPileClientRpc(card.GetComponent<NetworkObject>().NetworkObjectId, false);
        }

        private void AddToDrawPileServer(NetworkObjectReference cardReference)
        {
            if (cardReference.TryGet(out NetworkObject networkObject))
            {
                _drawPile.Add(cardReference);
                AddToPileClientRpc(networkObject.NetworkObjectId, false);
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

                cardNetworkObject.gameObject.transform.parent = pileObj.transform;
                cardNetworkObject.gameObject.transform.localPosition = Vector3.zero;
                cardNetworkObject.gameObject.transform.localRotation = Quaternion.identity;

                Debug.Log($"Setting {cardNetworkObject.gameObject.name} false");
                cardNetworkObject.gameObject.SetActive(false);
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

                // Clear the piles
                _playPile.Clear();
                _drawPile.Clear();
                deckObject.Clear();

                foreach (NetworkObjectReference cardRef in deck)
                {
                    if (cardRef.TryGet(out NetworkObject networkCard) && networkCard.IsSpawned)
                    {
                        networkCard.Despawn(true); // Despawn the card across the network
                    }
                }
                deck.Clear();
            }
        }


        [ClientRpc]
        private void ClearAllClientHandsClientRpc()
        {
            foreach (NetworkedHand hand in activeHands)
            {
                hand.Clear();  // Clients also clear their hands
            }
        }

        public void RequestDrawCard(GameObject card)
        {
            Debug.Log("Step 1");
            NetworkObject networkObject = card.GetComponent<NetworkObject>();
            NetworkObjectReference cardReference = new NetworkObjectReference(networkObject);

            DrawTopCardServerRpc(cardReference,  NetworkManager.Singleton.LocalClientId);

        }

        [ServerRpc(RequireOwnership = false)]
        private void DrawTopCardServerRpc(NetworkObjectReference cardReference, ulong clientId)
        {
            Debug.Log($"Server processing card draw request from client {clientId}.");
            if (IsServer)
            {
                // Remove the card from the draw pile
                //if (_drawPile.Count > 0)
                //{
                //    if(_drawPile[_drawPile.Count-1].TryGet(out NetworkObject topNetworkObject))
                //    {
                //        topNetworkObject == networkObject
                //    }
                //}


                Debug.Log("Step 2");
                if (cardReference.TryGet(out NetworkObject networkObject))
                {
                    if (_drawPile.Contains(cardReference))
                    {
                        _drawPile.Remove(cardReference);  // Remove from draw pile
                        Debug.Log($"Card {card.name} picked from draw pile.");

                        activeHands[currentHandIndex].DrawCardServerRpc(networkObject.gameObject); // This method is from NetworkedHand.cs

                        // Optionally trigger a client RPC to visually update clients on the draw action
                        UpdatePlayerHandClientRpc(cardReference, activeHands[currentHandIndex].NetworkObjectId);
                    }
                    else
                    {
                        Debug.Log("Card not found in draw pile.");
                    }

                }
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

            NetworkObjectReference cardReference = _playPile[_playPile.Count - 1];

            if (!cardReference.TryGet(out NetworkObject res))
            {
                Debug.Log("FATAL ERROR: Card not found at playPile(STARTCRZY8s)");
                return;
            }

            SetCardActiveClientRpc(cardReference.NetworkObjectId, true);

            Debug.Log("First card drawn.");
        }

        [ClientRpc]
        public void UpdatePlayerHandClientRpc(NetworkObjectReference cardReference, ulong handId)
        {
            if (cardReference.TryGet(out NetworkObject networkObject))
            {
                GameObject card = networkObject.gameObject;

                foreach (NetworkedHand hand in activeHands)
                {
                    if (hand.NetworkObjectId == handId)
                    {
                        hand.DrawCard(card);  // Add card to the correct hand on the client side
                        break;
                    }
                }
            }
        }

        public void PlayCard(GameObject card)
        {
            Debug.Log(card.name);



            if (!activeHands[currentHandIndex].heldCards.Contains(card)) // Card from wrong hand do not accept
            {
                Debug.Log("Wrong player!");
                return;
            }

            //if (!IsValidPlayCrazyEights(card))
            //{
            //    return;
            //}

            card.SetActive(false);
            card.GetComponent<Card>().played = true;
            card.GetComponent<XRGrabInteractable>().enabled = false;

            activeHands[currentHandIndex].heldCards.Remove(card);
            activeHands[currentHandIndex].ConfigureChildPositions();

            AddToPlayPileServer(card);

            if (_playPile.Count > 0)
            {
                NetworkObjectReference oldTopCardReference = _playPile[_playPile.Count - 1];
                if (oldTopCardReference.TryGet(out NetworkObject oldNetworkObject))
                {
                    GameObject oldTopCard = oldNetworkObject.gameObject;
                    oldTopCard.SetActive(false);
                }
            }

            NetworkObject newNetworkObject = card.GetComponent<NetworkObject>();
            NetworkObjectReference newCardReference = new NetworkObjectReference(newNetworkObject);
            _playPile.Add(newCardReference);

            if (_playPile.Count > 0)
            {
                NetworkObjectReference topCardReference = _playPile[_playPile.Count - 1];
                if (topCardReference.TryGet(out NetworkObject NetworkObject))
                {
                    GameObject topCard = NetworkObject.gameObject;
                    topCard.SetActive(false);
                }
            }

            UpdateCurrentIndex();
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
            Debug.Log("Card setup failed or Card is not valid");
            return false;
        }

        public void UpdateCurrentIndex()
        {
            if (currentHandIndex == activeHands.Count - 1) { currentHandIndex = 0; }
            else { currentHandIndex++; }

            Debug.Log("Setting current hand to " + activeHands[currentHandIndex].name);
        }

        public void CheckForPlayerWin()
        {
            foreach (NetworkedHand hand in activeHands)
            {
                if (hand.isEmpty())
                {
                    Debug.Log(hand.name + "is empty, calling courotine");
                    StartCoroutine(m_MiniGame.PlayerWonRoutine(hand.gameObject));
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
                    if (changeEvent.Value.TryGet(out NetworkObject noA))
                    {
                        deckObject.Add(noA.gameObject);
                    }
                    break;

                case NetworkListEvent<NetworkObjectReference>.EventType.Remove:
                    // A card was removed from the draw pile
                    Debug.Log($"Card removed from Deck: {changeEvent.Value}");
                    if (changeEvent.Value.TryGet(out NetworkObject noR))
                    {
                        deckObject.Remove(noR.gameObject);
                    }
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
                    break;

                case NetworkListEvent<NetworkObjectReference>.EventType.Remove:
                    // A card was removed from the draw pile
                    Debug.Log($"Card removed from draw pile: {changeEvent.Value}");
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
                    break;

                case NetworkListEvent<NetworkObjectReference>.EventType.Remove:
                    // A card was removed from the draw pile
                    Debug.Log($"Card removed from play pile: {changeEvent.Value}");
                    break;
            }
        }
    }
}

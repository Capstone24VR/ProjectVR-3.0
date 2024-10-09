using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static Card;
using Unity.Netcode;
using UnityEngine.XR.Content.Interaction;
using System.Linq;
using UnityEngine.XR;

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
        /// The number of starting cards
        /// </summary>
        [SerializeField] int startingHand = 5;

        [SerializeField] protected List<GameObject> deck = new List<GameObject>();

        [SerializeField] protected NetworkList<NetworkObjectReference> _drawPile = new NetworkList<NetworkObjectReference>();
        [SerializeField] protected NetworkList<NetworkObjectReference> _playPile = new NetworkList<NetworkObjectReference>();

        [SerializeField] protected int currentHandIndex;

        [SerializeField] protected List<NetworkedHand> activeHands = new List<NetworkedHand>();

        [SerializeField] private MiniGameManager miniManager;
        
        void Start()
        {
            TryGetComponent(out m_MiniGame);
            _drawPile.OnListChanged += OnDrawPileChanged;
            _playPile.OnListChanged += OnPlayPileChanged;
        }

        public void ResetGame()
        {
            StopAllCoroutines();
            RemoveGeneratedCards();
            activeHands.Clear();

            foreach(NetworkedHand hand in m_hands)
            {
                if(hand.active) { activeHands.Add(hand); }
            }

            CreateDeckServer();
            ShuffleDeckServer();
            InstatiateDrawPileServer();
        }

        public void StartGame()
        {
            Debug.Log(startingHand);
            for (int i = 0; i < startingHand; i++)
            {
                foreach (NetworkedHand hand in activeHands)
                {
                    if (hand.canDraw()) {
                        NetworkObjectReference topCard = _drawPile[_drawPile.Count-1];

                        if (topCard.TryGet(out NetworkObject networkCard)){
                            _drawPile.Remove(topCard);

                            if (!networkCard.IsSpawned)
                            {
                                networkCard.Spawn();
                            }
                            hand.AutoDrawCardServerRpc(topCard);
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

            if(_drawPile.Count > 0) {
                if(_drawPile[_drawPile.Count - 1].TryGet(out NetworkObject networkCard))
                {
                    networkCard.gameObject.SetActive(true);
                }
            }

        }

        public void EndGame()
        {
            StopAllCoroutines();
            RemoveGeneratedCards();
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
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Value value in Enum.GetValues(typeof(Value)))
                {
                    UnityEngine.Object pPrefab = ((int)value > 1 && (int)value < 11) ? 
                        Resources.Load("Free_Playing_Cards/PlayingCards_" + (int)value + suit) : // If One..Ten, parse into integer
                        Resources.Load("Free_Playing_Cards/PlayingCards_" + value + suit);      //  If J,Q,K

                    if (pPrefab == null)
                    {
                        Debug.LogError("Prefab not found: " + "Free_Playing_Cards/PlayingCards_" + value + suit);
                        continue;
                    }


                    GameObject newCard = Instantiate(card, drawPileObj.transform, false); // Create card
                    var netWorkObject = newCard.GetComponent<NetworkObject>(); // get network object for server spawning

                    if(netWorkObject != null)
                    {
                        netWorkObject.Spawn();
                    }

                    // Visual Model Creation
                    GameObject model = (GameObject)Instantiate(pPrefab, newCard.transform, false);
                    if (model == null)
                    {
                        Debug.LogError("Model instantiation failed.");
                        continue;
                    }

                    model.transform.rotation = Quaternion.identity;
                    model.transform.localPosition = Vector3.zero;

                    // Setting Card Value, Suite and name
                    newCard.GetComponent<Card>().suit = suit;
                    newCard.GetComponent<Card>().value = value;
                    newCard.name = "Card: " + suit + " " + value;
                    newCard.SetActive(false); // hiding card
                    AddToDrawPile(newCard);
                }
            }
            Debug.Log("Deck created SeverSide. Notifying Clients . . .");
            yield return new WaitForSeconds(0.5f);

            CreateDeckClientRpc();
        }


        /// <summary>
        /// Creates deck on the clients.
        /// </summary>
        [ClientRpc]
        public void CreateDeckClientRpc()
        {
            StartCoroutine(CreateDeckOnClient());
        }

        IEnumerator CreateDeckOnClient()
        {
            Debug.Log("Client received deck creation. Waiting for server to finish...");

            yield return new WaitForSeconds(1.0f); // Ensure enough delay for server to fully spawn cards

            Debug.Log("Creating Deck on Client...");
            foreach (NetworkObjectReference cardReference in _drawPile)  // Assuming `drawPile` is synchronized on the client
            {
                var cardComponent = card.GetComponent<Card>();

                UnityEngine.Object pPrefab = ((int)cardComponent.value > 1 && (int)cardComponent.value < 11) ?
                    Resources.Load("Free_Playing_Cards/PlayingCards_" + (int)cardComponent.value + cardComponent.suit) :
                    Resources.Load("Free_Playing_Cards/PlayingCards_" + cardComponent.value + cardComponent.suit);

                if (pPrefab == null)
                {
                    Debug.LogError("Prefab not found: " + "Free_Playing_Cards/PlayingCards_" + cardComponent.value + cardComponent.suit);
                    continue;
                }

                // Instantiate model on the client
                GameObject model = (GameObject)Instantiate(pPrefab, card.transform, false);
                if (model == null)
                {
                    Debug.LogError("Model instantiation failed.");
                    continue;
                }

                model.transform.rotation = Quaternion.identity;
                model.transform.localPosition = Vector3.zero;
            }

            Debug.Log("Client deck created.");
            yield return null;
        }


        /// <summary>
        /// Shuffles deck on the server.
        /// </summary>
        public void ShuffleDeckServer()
        {
            if (IsServer)
            {
                List<int> shuffledIndices = ShuffleDeckIndices(deck.Count);
                ShuffleDeckClientRpc(shuffledIndices.ToArray());
            }
        }

        /// <summary>
        /// Shuffles deck on the clients.
        /// </summary>
        [ClientRpc]
        public void ShuffleDeckClientRpc(int[] shuffledIndices)
        {
            Debug.Log("Shuffling Deck . . .");
            List<GameObject> shuffledDeck = new List<GameObject>(shuffledIndices.Length);
            foreach(int index in shuffledIndices)
            {
                shuffledDeck.Add(deck[index]);
            }
            deck = shuffledDeck;
            Debug.Log("Deck Shuffled.");
        }

        /// <summary>
        /// Shuffles the indexes of the deck
        /// </summary>
        /// <param name="count">size of deck</param>
        /// <returns></returns>
        private List<int> ShuffleDeckIndices(int count)
        {
            List<int> indices = Enumerable.Range(0,count).ToList();
            System.Random r = new System.Random();

            Debug.Log(count);
            for (int n = count - 1; n > 0; --n)
            {
                int k = r.Next(n + 1);
                int temp = indices[n];
                indices[n] = indices[k];
                indices[k] = temp;
            }
            return indices;
        }

        /// <summary>
        /// Instatitates draw pile on the server.
        /// </summary>
        public void InstatiateDrawPileServer()
        {
            if (IsServer)
                InstatiateDrawPileServerClientRpc();
        }

        /// <summary>
        /// Creates deck on the clients.
        /// </summary>
        [ClientRpc]
        void InstatiateDrawPileServerClientRpc()
        {
            Debug.Log("Creating Draw Pile . . .");
            foreach (GameObject card in deck)
            {
                NetworkObject networkObject = card.GetComponent<NetworkObject>();
                NetworkObjectReference cardReference = new NetworkObjectReference(networkObject);
                _drawPile.Add(cardReference);
            }
            Debug.Log("Draw Pile created.");
        }


        public void ManualDrawCard(GameObject card)
        {
            if (_drawPile.Count > 0)
            {
                long playerId = miniManager.GetLocalPlayerID();
                Debug.Log(activeHands[currentHandIndex].ownerManager.HandOwnerId);

                NetworkObjectReference cardReference = _drawPile[_drawPile.Count - 1];
                if(cardReference.TryGet(out NetworkObject networkObject))
                {
                    GameObject topCard = networkObject.gameObject;
                    if (card == topCard)
                    {
                        activeHands[currentHandIndex].ManDrawCard(topCard);
                        _drawPile.Remove(cardReference);

                        if (_drawPile.Count > 0) {
                            NetworkObjectReference newCardReference = _drawPile[_drawPile.Count - 1];
                            if (newCardReference.TryGet(out NetworkObject newNetworkObject))
                            {
                                GameObject newTopCard = newNetworkObject.gameObject;
                                newTopCard.SetActive(true);
                            }
                            else
                            {
                                Debug.Log("Error: cannot get new top card object");
                                return;
                            }
                        }
                        if (!IsValidPlayCrazyEights(card)) // Checking if newly drawn card is valid
                        {
                            UpdateCurrentIndex(); // If not valid pass your turn
                        }
                    }
                }
                else
                {
                    Debug.Log("Error: cannot get card for manual drawing");
                    return;
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

            AddToPlayPile(card);

            if (_playPile.Count > 0) {
                NetworkObjectReference oldTopCardReference = _playPile[_playPile.Count - 1];
                if(oldTopCardReference.TryGet(out NetworkObject oldNetworkObject))
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

        protected void StartCrazyEights()
        {
            NetworkObjectReference firstReference = _drawPile[_drawPile.Count - 1];
            if (firstReference.TryGet(out NetworkObject networkCardDraw))
            {
                _drawPile.Remove(firstReference);
                GameObject firstCard = networkCardDraw.gameObject;
                Debug.Log("Drawing First card(" + firstCard.name + ") for Crazy Eights . . . ");
                AddToPlayPile(firstCard);
            }
            else
            {
                Debug.Log("FATAL ERROR: Card not found at drawPiile(STARTCRZY8s)");
                return;
            }



            if (_playPile.Count > 0)
            {
                NetworkObjectReference cardReference = _playPile[_playPile.Count - 1];
                if (cardReference.TryGet(out NetworkObject networkCardPlay))
                {
                    GameObject topCard = networkCardPlay.gameObject;
                    topCard.SetActive(true);
                }
                else
                {
                    Debug.Log("FATAL ERROR: Card not found at playPile(STARTCRZY8s)");
                    return;
                }
            }

            Debug.Log("First card drawn.");
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

        private void AddToPile(GameObject card, Transform pileObj, NetworkList<NetworkObjectReference> pile)
        {
            card.transform.parent = pileObj;
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;

            NetworkObject networkObject = card.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned)
            {
                NetworkObjectReference cardReference = new NetworkObjectReference(networkObject);
                pile.Add(cardReference);
            }
            else
            {
                Debug.Log($"Catastrophic Error: Could not add {card.name} to pile");
            }
        }

        private void AddToPlayPile(GameObject card)
        {
            AddToPile(card, drawPileObj.transform, _playPile);
        }

        private void AddToDrawPile(GameObject card)
        {
            AddToPile(card, drawPileObj.transform, _drawPile);
        }

        private void RemoveGeneratedCards()
        {
            foreach (NetworkedHand hand in activeHands)
            {
                hand.Clear();
            }

            _playPile.Clear();
            _drawPile.Clear();

            foreach (GameObject card in deck)
            {
                Destroy(card);
            }
            deck.Clear();
        }
    }
}

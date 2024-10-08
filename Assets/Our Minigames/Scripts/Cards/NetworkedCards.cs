using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static Card;
using Unity.Netcode;
using UnityEngine.XR.Content.Interaction;
using System.Linq;

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

        [SerializeField] protected Stack<GameObject> _drawPile = new Stack<GameObject>();
        [SerializeField] protected Stack<GameObject> _playPile = new Stack<GameObject>();

        [SerializeField] protected int currentHandIndex;

        [SerializeField] protected List<NetworkedHand> activeHands = new List<NetworkedHand>();

        [SerializeField] private MiniGameManager miniManager;
        
        void Start()
        {
            TryGetComponent(out m_MiniGame);
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
                        GameObject topCard = _drawPile.Pop();
                        NetworkObject networkCard = topCard.GetComponent<NetworkObject>();

                        if (!networkCard.IsSpawned)
                        {
                            networkCard.Spawn();
                        }

                        NetworkObjectReference cardReference = new NetworkObjectReference(networkCard);
                        hand.AutoDrawCardServerRpc(cardReference); 
                    }
                }
            }
            currentHandIndex = 0;
            StartCrazyEights();
            _drawPile.Peek().SetActive(true);
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
                CreateDeckClientRpc();
        }

        /// <summary>
        /// Creates deck on the clients.
        /// </summary>
        [ClientRpc]
        public void CreateDeckClientRpc()
        {
            StartCoroutine(CreateDeck());
        }


        IEnumerator CreateDeck()
        {
            Debug.Log("Creating Deck . . .");
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Value value in Enum.GetValues(typeof(Value)))
                {
                    UnityEngine.Object pPrefab = ((int)value > 1 && (int)value < 11) ? 
                        Resources.Load("Free_Playing_Cards/PlayingCards_" + (int)value + suit) : // If One..Ten, parse into integer
                        Resources.Load("Free_Playing_Cards/PlayingCards_" + value + suit);      //  If J,Q,K

                    GameObject newCard = Instantiate(card, drawPileObj.transform, false);
                    newCard.transform.localPosition = Vector3.zero;
                    GameObject model = (GameObject)Instantiate(pPrefab, newCard.transform, false);
                    model.transform.rotation = Quaternion.identity;
                    model.transform.localPosition = Vector3.zero;
                    newCard.GetComponent<Card>().suit = suit;
                    newCard.GetComponent<Card>().value = value;
                    newCard.name = "Card: " + suit + " " + value;
                    newCard.SetActive(false);
                    deck.Add(newCard);
                }
            }
            Debug.Log("Deck created.");
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
                _drawPile.Push(card);
            }
            Debug.Log("Draw Pile created.");
        }


        public void ManualDrawCard(GameObject card)
        {
            if (_drawPile.Count > 0)
            {
                long playerId = miniManager.GetLocalPlayerID();
                Debug.Log(activeHands[currentHandIndex].ownerManager.HandOwnerId);

                var topCard = _drawPile.Peek();
                if (card == topCard)
                {
                    activeHands[currentHandIndex].ManDrawCard(topCard);
                    _drawPile.Pop();
                    if (_drawPile.Count > 0) { _drawPile.Peek().SetActive(true); }
                    if (!IsValidPlayCrazyEights(card)) // Checking if newly drawn card is valid
                    {
                        UpdateCurrentIndex(); // If not valid pass your turn
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

            AddToPlayPile(card);

            if (_playPile.Count > 0) { _playPile.Peek().SetActive(false); }
            _playPile.Push(card);
            _playPile.Peek().SetActive(true);

            UpdateCurrentIndex();
        }

        protected void StartCrazyEights()
        {
            GameObject firstCard = _drawPile.Pop();
            Debug.Log("Drawing First card(" + firstCard.name + ") for Crazy Eights . . . ");
            AddToPlayPile(firstCard);

            if (_playPile.TryPeek(out GameObject topCard))
            {
                topCard.SetActive(true);
            }

            Debug.Log("First card drawn.");
        }

        protected bool IsValidPlayCrazyEights(GameObject card)
        {
            if (_playPile.TryPeek(out GameObject topCard))
            {
                if (topCard.GetComponent<Card>().suit == card.GetComponent<Card>().suit)
                {
                    Debug.Log("Cards share the same suit: " + card.GetComponent<Card>().suit);
                    return true;
                }
                else if (topCard.GetComponent<Card>().value == card.GetComponent<Card>().value)
                {
                    Debug.Log("Cards share the same value: " + card.GetComponent<Card>().value);
                    return true;
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

        private void AddToPlayPile(GameObject card)
        {
            card.transform.parent = playPileObj.transform;
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;
            card.GetComponent<Card>().played = true;
            card.GetComponent<XRGrabInteractable>().enabled = false;
            _playPile.Push(card);
        }

        private void AddToDrawPile(GameObject card)
        {
            card.transform.parent = drawPileObj.transform;
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;
            _drawPile.Push(card);
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

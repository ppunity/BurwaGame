
using CardGame;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for Linq methods like ToList()
using Random = UnityEngine.Random;
using TMPro; // Use Unity's Random for game randomness
using Photon.Pun;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CardManager : MonoBehaviour
{
    // --- Static Singleton Reference ---
    
    // The static field that holds the single instance of the CardManager.
    public static CardManager Instance { get; private set; } 

    private PhotonView photonView;

    public string masterClientTag;

    public enum GameState { READY, WAIT, SWITCH_MASTER, WIN, LOSE };
    public GameState gameState;

    public enum WhichPlayer { ME, OTHER };

    public WhichPlayer whichPlayer;


    public Card cardPrefab;

    public GameObject Selection;
    public GameObject Pack;
    public GameObject DisCards;
    public GameObject Hand_P1;
    public GameObject Hand_P2;


    public GameObject winText;

    public bool trumpSelected = false;
    public bool gameOver = false;

    private int turnId_;


    int TurnId
    {
        get { return turnId_; }
        set { turnId_ = value; }
    }



    private struct CardData
    {
        public string Symbol;
        public string Value;
        
        public CardData(string symbol, string value)
        {
            Symbol = symbol;
            Value = value;
        }
    }

    private void Awake()
    {
        // 1. Enforce Singleton: Ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("CardManager: Found more than one instance. Destroying this duplicate.");
            Destroy(this.gameObject);
            return;
        }
        
        // 2. Set the static reference to this instance.
        Instance = this;
        // Optional: If you want the manager to persist across scene loads, add DontDestroyOnLoad(gameObject);

        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            photonView = gameObject.AddComponent<PhotonView>();
        }
    
    }

    
    // --- Card Attribute Data (Rest of the code remains the same) ---
    
    [Header("Card Attributes")]
    public List<string> cardSymbols = new List<string> { "Hearts", "Diamonds", "Clubs", "Spades" };
    public List<string> cardValues = new List<string> { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" ,"A",};

    // --- Trump State ---
    
    [Header("Game State")]
    [Tooltip("The currently selected trump symbol.")]
    public string currentTrumpSymbol = "None";
    
    [Tooltip("The currently selected trump value (e.g., for 'Wild Cards').")]
    public string currentTrumpValue = "None";

    [Tooltip("Tracks whose turn it is to receive a card.")]
    private bool isPlayerOneTurn = true; // Start with Player 1
    // --- Public Functions ---



    void Start()
    {
        masterClientTag = PhotonNetwork.LocalPlayer.CustomProperties["tag"].ToString();
        // Any initialization logic can go here.
       SetGame(masterClientTag);

       if(!PhotonNetwork.IsMasterClient)
        {
            whichPlayer = WhichPlayer.OTHER;
        }
        else
        {
            whichPlayer = WhichPlayer.ME;
        }

    }

    public void SetGame(string myTag)
    {
        if(myTag == "Dealer")
        {
            isPlayerOneTurn = true;

            CreateSelection();            
            Selection.SetActive(true);
        }
        else
        {
            isPlayerOneTurn = false;
            Selection.SetActive(false);
        }

        turnId_ = 0;
        gameOver = false;
        isPlayerOneTurn = true;
        trumpSelected = false;
        currentTrumpSymbol = "None";
        currentTrumpValue = "None";
        Pack.SetActive(false);
        winText.SetActive(false);
        SetHand1Cards();
        SetHand2Cards();
        ClearDiscards();
        
    }
    


    /// <summary>
    /// Executes the action when a Card is clicked.
    /// </summary>
    /// <param name="cardScript">The Card script instance that was clicked.</param>
    public void OnTrumpCardClicked(Card cardScript)
    {
        // Check if the clicked card belongs to the 'Selection' context (based on its parent)
        if (cardScript.transform.parent != null && cardScript.transform.parent.gameObject == Selection)
        {
            // If it's a selection card, select its symbol as trump
            SelectTrumpValue(cardScript.CardValue);
            RemoveOtherSelectionCards(cardScript);
            cardScript.UpdateCardVisual();        
            SendTrumpSelectedRPC(cardScript.CardValue); 

        }
        else
        {
            // Normal game card logic
            Debug.Log($"Card Played: {cardScript.CardValue} of {cardScript.CardSymbol}");
        }

       
    }


    void SendTrumpSelectedRPC(string value)
    {
        photonView.RPC("ReceiveTrumpSelectedRPC", RpcTarget.Others, value);
    }

    void ReceiveTrumpSelectedRPC(string value)
    {
        Selection.SetActive(true);        
        SelectTrumpValue(value);
        RemoveOtherSelectionCards(null); // Remove all selection cards
        CreateFullPack();

    }


    

// --- Updated OnPackCardClicked ---
public void OnPackCardClicked(Card cardScript)
{
    // 1. Authority Check: Ensure the current player is the one whose turn it is.
    // Assuming 'WhichPlayer.ME' is P1 when isPlayerOneTurn is true, and P2 when false.
    bool isMyTurn = (isPlayerOneTurn && whichPlayer == WhichPlayer.ME) || (!isPlayerOneTurn && whichPlayer == WhichPlayer.OTHER);
    
    // Further complex check needed here if you have more than 2 players, 
    // but for 2 players, this simple flip logic works if 'ME' is always P1.
    // For simplicity, let's assume the player who is CURRENTLY allowed to click
    // is the one whose local turn it is.
    if (isPlayerOneTurn && masterClientTag != "Dealer") // Example logic for turn control
    {
        // This is where you would put the specific turn-check logic for your game.
        // If it's not the local player's turn to draw, exit.
        // return; 
    }
    
    // Safety check for game state
    if (gameOver || !trumpSelected)
    {
        Debug.Log("Game is over or trump not selected. No more cards can be dealt.");
        return;
    }
    
    // Ensure only the Master Client (or the authorized player) initiates the move via RPC.
    // A robust game might only allow the active player to click, then they send the RPC.
    
    // For simplicity in a 2-player draw game, we'll let the active player click and send the RPC.
    if (PhotonNetwork.InRoom)
    {
        // Send the card's unique name to all clients (including the local client via RpcTarget.All)
        photonView.RPC("SynchronizeCardDrawRPC", RpcTarget.All, cardScript.name);
    }
    else
    {
        // Fallback for single-player testing (no RPC needed)
        ExecuteCardDraw(cardScript);
    }
}

// --- New Synchronized Execution Method ---
/// <summary>
/// Contains the core logic for drawing a card, moving it to the hand/discard,
/// and checking for game end. This is called locally and via RPC on all clients.
/// </summary>
/// <param name="cardScript">The Card instance to be processed.</param>
private void ExecuteCardDraw(Card cardScript)
{
    // Logic for Discarding the first two cards (turnId_ = 0 and 1)
    if (turnId_ < 2)
    {
        MoveCard(cardScript, DisCards.transform, Card.CardType.Discard);
        Debug.Log($"Card {cardScript.name} moved to Discard (Turn {turnId_}).");
    }
    // Logic for Dealing to Hands (turnId_ >= 2)
    else 
    {
        // Determine the target hand GameObject and the new CardType
        GameObject targetHand;
        Card.CardType newType;

        if (isPlayerOneTurn)
        {
            targetHand = Hand_P1;
            newType = Card.CardType.Hand_P1;
        }
        else
        {
            targetHand = Hand_P2;
            newType = Card.CardType.Hand_P2;
        }

        // 1. Move the card to the target hand
        MoveCard(cardScript, targetHand.transform, newType);
        
        Debug.Log($"{cardScript.name} dealt to {(isPlayerOneTurn ? "Player 1" : "Player 2")}'s Hand.");

        // 2. Check for the Win condition *before* toggling the turn
        if(cardScript.CardValue == currentTrumpValue)
        {
            // Note: isPlayerOneTurn is TRUE if P1 just drew the winning card
            string winner = isPlayerOneTurn ? "Player 1" : "Player 2";

            gameOver = true;
            winText.SetActive(true);
            winText.GetComponent<TextMeshProUGUI>().text = $"{winner} Wins!";
            Pack.SetActive(false);
            
            Debug.Log($"{winner} wins by drawing the Trump Value Card!");
        }

        // 3. Toggle the turn flag for the next card (happens on ALL clients)
        isPlayerOneTurn = !isPlayerOneTurn;
    }
    
    // 4. Increment the turn ID (happens on ALL clients)
    turnId_++;

    // 5. Update Visuals
    cardScript.UpdateCardVisual();

    Debug.Log($"State Sync: Turn ID is now {turnId_}. Next turn: {(isPlayerOneTurn ? "Player 1" : "Player 2")}");
}





    /// <summary>
    /// Helper function to handle the physical movement and attribute update of a card.
    /// </summary>
    /// <param name="card">The card to move.</param>
    /// <param name="newParent">The new parent transform (e.g., Hand_P1).</param>
    /// <param name="newType">The new CardType (e.g., Hand_P1).</param>
    public void MoveCard(Card card, Transform newParent, Card.CardType newType)
    {
        // 1. Change the card's parent/location
        card.transform.SetParent(newParent);

        // 2. Update the card's type
        card.cardtype = newType;

        // Note: If you use the WorldSpaceGridLayout, moving the card to a new parent
        // will automatically trigger the layout script on that new parent to reposition the card!
    }

    public void RemoveOtherSelectionCards(Card cardToKeep)
    {
        List<GameObject> cardsToRemove = new List<GameObject>();

        foreach (Transform child in Selection.transform)
        {
            cardsToRemove.Add(child.gameObject);
        }

        foreach (GameObject card in cardsToRemove)
        {
            if (card != cardToKeep.gameObject) 
            {
            Destroy(card);
            }
        }
    }

    /// <summary>
    /// Function to select the Trump Symbol based on a clicked selection card.
    /// </summary>
    public void SelectTrumpSymbol(string value)
    {
        if (cardSymbols.Contains(value))
        {
            currentTrumpValue = value;
            trumpSelected = true;
            
            Debug.Log($"!!! TRUMP SYMBOL SELECTED: {currentTrumpSymbol} !!!");
        }
        else
        {
            Debug.LogError($"Attempted to set an invalid trump symbol: {value}");
        }

        //Pack.SetActive(true);
    }
    
    /// <summary>
    /// Function to select the Trump Value.
    /// </summary>
    public void SelectTrumpValue(string value)
    {
        if (cardValues.Contains(value))
        {
            currentTrumpValue = value;
            Debug.Log($"Trump Value Selected: {currentTrumpValue}");
            trumpSelected = true;
            Pack.SetActive(true);
        }
        else
        {
            Debug.LogError($"Attempted to set an invalid trump value: {value}");
        }
    }

    public void SetHand1Cards()
    {
        if (Hand_P1 == null || cardPrefab == null)
        {
            
            return;
        }

        // Clear any existing children from the Selection object before populating
        foreach (Transform child in Hand_P1.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void SetHand2Cards()
    {
        if (Hand_P2 == null || cardPrefab == null)
        {
            
            return;
        }

        // Clear any existing children from the Selection object before populating
        foreach (Transform child in Hand_P2.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void ClearDiscards()
    {
        if (DisCards == null || cardPrefab == null)
        {
            
            return;
        }

        // Clear any existing children from the Selection object before populating
        foreach (Transform child in DisCards.transform)
        {
            Destroy(child.gameObject);
        }
    }


    /// <summary>
    /// Creates the 13 selection cards based only on the cardValues list.
    /// These cards will only show their value, and when clicked, will set trump symbol.
    /// </summary>
    [ContextMenu("Create Selection Cards")]
    public void CreateSelection()
    {
        if (Selection == null || cardPrefab == null)
        {
            Debug.LogError("Selection GameObject or Card Prefab not assigned in CardManager.");
            return;
        }

        // Clear any existing children from the Selection object before populating
        foreach (Transform child in Selection.transform)
        {
            Destroy(child.gameObject);
        }

        // Use a default symbol for selection cards (e.g., the first in the list)
        // Note: The Card.cs logic uses the CardSymbol, even if it's visually hidden.
        string defaultSymbol = cardSymbols.Count > 0 ? cardSymbols[0] : "Default";

        foreach (string value in cardValues)
        {
            // Create and configure a card for each value
            Card newCard = InstantiateCard(null, value, Selection.transform, Card.CardType.Selection);

            // Optional: The selection cards only need a value for display. 
            // We set the Symbol for the trump selection logic, but visually they might differ.
        }

        Debug.Log($"Created {cardValues.Count} selection cards in the '{Selection.name}' object.");
    }




    /// <summary>
    /// Creates the full 52-card deck, shuffles it, places it in the Pack, 
    /// and sends the shuffled order to the other player via RPC.
    /// This should ONLY be called by the Master Client/Dealer.
    /// </summary>
    [ContextMenu("Create Full Pack (Shuffled)")]
    public void CreateFullPack()
    {
        if (Pack == null || cardPrefab == null)
        {
            Debug.LogError("Pack GameObject or Card Prefab not assigned in CardManager.");
            return;
        }
        
        // 1. Clear any existing children from the Pack object
        foreach (Transform child in Pack.transform)
        {
            Destroy(child.gameObject);
        }

        // 2. Generate the unshuffled list of all card combinations
        List<CardData> unshuffledDeck = new List<CardData>();
        foreach (string symbol in cardSymbols)
        {
            foreach (string value in cardValues)
            {
                unshuffledDeck.Add(new CardData(symbol, value));
            }
        }
        
        // 3. Shuffle the list using the helper function
        List<CardData> shuffledDeck = ShuffleList(unshuffledDeck);
        
        // --- SERIALIZATION FOR RPC ---
        // Convert the list of structs into separate arrays of strings for the RPC
        string[] symbols = shuffledDeck.Select(cd => cd.Symbol).ToArray();
        string[] values = shuffledDeck.Select(cd => cd.Value).ToArray();

        // 4. Instantiate cards from the shuffled list locally
        InstantiatePackFromData(shuffledDeck);
        
        Debug.Log($"Created a full deck of {shuffledDeck.Count} cards in a **randomized** order in the '{Pack.name}' object.");

        // 5. Send the shuffled deck data to the other player
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            // Note: Sending two separate string arrays is generally safer/easier than trying to serialize a custom List<struct>
            photonView.RPC("ReceiveShuffledPackRPC", RpcTarget.Others, symbols, values);
            Debug.Log("Sent shuffled deck data to opponent via RPC.");
        }
    }

    /// <summary>
    /// RPC method to receive the shuffled deck data from the Master Client.
    /// </summary>
    /// <param name="symbols">Array of card symbols.</param>
    /// <param name="values">Array of card values (ranks).</param>
    [PunRPC]
    public void ReceiveShuffledPackRPC(string[] symbols, string[] values)
    {
        Debug.Log("Received shuffled deck data from Master Client via RPC.");

        // 1. Reconstruct the CardData list from the received arrays
        if (symbols == null || values == null || symbols.Length != values.Length)
        {
            Debug.LogError("Received invalid deck data in RPC.");
            return;
        }

        List<CardData> receivedDeck = new List<CardData>();
        for (int i = 0; i < symbols.Length; i++)
        {
            receivedDeck.Add(new CardData(symbols[i], values[i]));
        }

        // 2. Instantiate the pack locally based on the received data
        InstantiatePackFromData(receivedDeck);
        
        // Now, both players have an identical, correctly ordered deck in their Pack GameObject.
    }

    /// <summary>
    /// Instantiates the actual Card GameObjects into the Pack container 
    /// from a provided list of card data.
    /// </summary>
    /// <param name="deckData">The ordered list of cards to instantiate.</param>
    private void InstantiatePackFromData(List<CardData> deckData)
    {
        // Clear any existing children from the Pack object first
        foreach (Transform child in Pack.transform)
        {
            Destroy(child.gameObject);
        }

        // Instantiate the cards
        foreach (CardData cardData in deckData)
        {
            InstantiateCard(
                symbol: cardData.Symbol, 
                value: cardData.Value, 
                parent: Pack.transform, 
                type: Card.CardType.Pack
            );
        }
    }


    /// <summary>
    /// Implements the Fisher-Yates shuffle algorithm on a generic list.
    /// </summary>
    private List<T> ShuffleList<T>(List<T> list)
    {
        // Create a copy to avoid modifying the original list if it were passed by reference
        List<T> shuffledList = list.ToList();
        
        // The standard Fisher-Yates loop runs from the end of the list to the start.
        for (int i = shuffledList.Count - 1; i > 0; i--)
        {
            // Pick a random element between 0 and i (inclusive)
            int randomIndex = Random.Range(0, i + 1);

            // Swap the element at i with the randomly chosen element
            T temp = shuffledList[i];
            shuffledList[i] = shuffledList[randomIndex];
            shuffledList[randomIndex] = temp;
        }
        return shuffledList;
    }


    /// <summary>
    /// Helper function to handle the instantiation and setting of card attributes.
    /// </summary>
    /// <param name="symbol">The card's suit/symbol.</param>
    /// <param name="value">The card's rank/value.</param>
    /// <param name="parent">The transform the new card should be parented to.</param>
    /// <returns>The newly created Card component.</returns>
    public Card InstantiateCard(string symbol, string value, Transform parent, Card.CardType type)
    {
        if(symbol == null)
        {
            symbol = "None";
        }
        if(value == null)
        {
            value = "None";
        }
        Card newCard = Instantiate(cardPrefab, parent);
        
        // Name the game object for easy identification in the Hierarchy
        newCard.name = $"{value} of {symbol}";
        
        // Set the attributes using the public setter function from Card.cs
        newCard.SetAttributes(symbol, value, type);
        newCard.UpdateCardVisual();
        
        return newCard;
    }

    public void CheckRoomPlayers()
    {
        if (gameState == GameState.WIN || gameState == GameState.LOSE) return;

        if (PhotonNetwork.PlayerList.Length == 1)
        {
            gameState = GameState.WIN;
            GameOver();
        }
    }

    public void GameOver()
    {
        Debug.Log("Game Over!");
    }

    public void CheckTurn()
    {
        
    }
}
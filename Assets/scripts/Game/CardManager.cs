
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
using System;
using Unity.VisualScripting;
using Photon.Realtime;

public class CardManager : MonoBehaviour
{

    [Header("AutoDeal")]
    public bool AutoDeal = false;

    

    // The static field that holds the single instance of the CardManager.
    public static CardManager Instance { get; private set; } 

    private PhotonView photonView;

    public string RoomName;

    public string masterClientTag;

    public enum GameState { READY, WAIT, SWITCH_MASTER, WIN, LOSE, PLAYING };
    public GameState gameState;

    public Card cardPrefab;

    public GameObject Selection;
    public GameObject Pack;
    public GameObject DisCards;
    public GameObject Hand_P1;
    public GameObject Hand_P2;

    public GameObject[] MyObjects;


    public GameObject winText;

    public bool trumpSelected = false;
    public bool gameOver = false;

    public bool isDealing = false;

    
    private int turnId_;

    private bool dealFromTop = true;

    [SerializeField] private GameObject CurrentValuePanel;
    [SerializeField] private TextMeshProUGUI CurrentValueText;
    [SerializeField] private GameObject shufflePanel;   
    [SerializeField] private GameObject CutPanel;
    [SerializeField] private GameObject StatusPanel;
    [SerializeField] private GameObject DealPanel;
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] TextMeshProUGUI PriceText;
    [SerializeField] private GameObject OrderSelecrtionPanel;
    [SerializeField] private Animator MyAnimator;
    [SerializeField] private Animator OpponentAnimator;
    [SerializeField] private GameObject MyAnimationCard;
    [SerializeField] private GameObject OpponentAnimationCard;
    [SerializeField] private GameObject fadePanel;

    [SerializeField] private GameObject TimerPanel;
    [SerializeField] private Image Clock;
    [SerializeField] private TextMeshProUGUI TimeText;
    private Coroutine CurrntTimer;

    [SerializeField] private GameObject WinVid;
    [SerializeField] private GameObject LossVid;

    private Animator DealerAnimator;
    private Animator NoneDealerAnimator;

    private GameObject DealingCard;

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

    private int cardIndex;

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

        RoomName = PhotonController.Instance.whichRoom;

        if(RoomName == "galle")
        {
            PriceText.text = "200";
        }
        else if(RoomName == "kandy")
        {
            PriceText.text = "400";
        }
        else if(RoomName == "colombo")
        {
            PriceText.text = "1000";
        }
        else if(RoomName == "jaffna")
        {
            PriceText.text = "2000";
        }
        else if(RoomName == "sigiri")
        {
            PriceText.text = "10000";
        }
        
    }

    public void SetGame(string myTag)
    {
        cardIndex = 0;

        Vector3 pos1 = Hand_P1.transform.parent.position;
        Vector3 pos2 = Hand_P2.transform.parent.position;

        if(myTag == "Dealer")
        {   
            Hand_P1.transform.parent.position = pos2;
            Hand_P2.transform.parent.position = pos1;            
            
            isPlayerOneTurn = false;
            Selection.SetActive(false);


            shufflePanel.SetActive(true);
            if(CurrntTimer != null)
            {
                StopCoroutine(CurrntTimer);
            }
            CurrntTimer = StartCoroutine(TimerCoroutine(15f, 5f, Shuffle));
            
            StatusPanel.SetActive(false);
            DealerAnimator = MyAnimator;
            NoneDealerAnimator = OpponentAnimator;
            DealingCard = MyAnimationCard;
            
            
        }
        else
        {
            
            isPlayerOneTurn = true;

            CreateSelection();            
            Selection.SetActive(false);

            shufflePanel.SetActive(false);
            StatusPanel.SetActive(true);
            statusText.text = "Waiting Opponent to Shuffle";

            DealerAnimator = OpponentAnimator;
            NoneDealerAnimator = MyAnimator;
            DealingCard = OpponentAnimationCard;
            

            
        }

        CurrentValuePanel.SetActive(false);

        Hand_P1.transform.parent.gameObject.SetActive(false);
        Hand_P2.transform.parent.gameObject.SetActive(false);
        foreach (GameObject obj in MyObjects)
        {
            obj.SetActive(false);
        }


        OrderSelecrtionPanel.SetActive(false);

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

    public void Shuffle()
    {
        if(CurrntTimer != null)
        {
            StopCoroutine(CurrntTimer);
        }
        
        photonView.RPC("ReceiveShuffleRPC", RpcTarget.All);        
    }

    [PunRPC]
    void ReceiveShuffleRPC()
    {        
        StartCoroutine(ShuffleCoroutine());
    }

    IEnumerator ShuffleCoroutine()
    {
        shufflePanel.SetActive(false);
        StatusPanel.SetActive(false);
        DealerAnimator.SetTrigger("shuffle");
        yield return new WaitForSeconds(3f);
        fadePanel.SetActive(true);
        yield return new WaitForSeconds(1f);                
        DealerAnimator.SetTrigger("shuffleDone");
        if(masterClientTag == "Dealer")
        {
            StatusPanel.SetActive(true);
            statusText.text = "Waiting for Opponent to Cut";
        }
        else
        {
            CutPanel.SetActive(true);
            if(CurrntTimer != null)
            {
                StopCoroutine(CurrntTimer);
            }
            CurrntTimer = StartCoroutine(TimerCoroutine(15f, 5f, Cut));

        }
    }

    public void Cut()
    {
        if(CurrntTimer != null)
        {
            StopCoroutine(CurrntTimer);
        }
        photonView.RPC("ReceiveCutRPC", RpcTarget.All);        
    }

    [PunRPC]
    void ReceiveCutRPC()
    {        
        StartCoroutine(CutCoroutine());
    }

    IEnumerator CutCoroutine()
    {
        CutPanel.SetActive(false);
        StatusPanel.SetActive(false);
        NoneDealerAnimator.SetTrigger("cut");
        yield return new WaitForSeconds(3f);
        fadePanel.SetActive(true);
        yield return new WaitForSeconds(1f);

        if(masterClientTag == "Dealer")
        {
            StatusPanel.SetActive(true);
            statusText.text = "Waiting for Opponent to Select Card";
        }
        else
        {
            Selection.SetActive(true);

            if(CurrntTimer != null)
            {
                StopCoroutine(CurrntTimer);
            }
            CurrntTimer = StartCoroutine(TimerCoroutine(15f, 5f, AutomaticSetTrumpCard));

        }
    }


    public void AutomaticSetDealFromTop()
    {
        SetDealFromTop(true);
    }

    public void SetDealFromTop(bool fromTop)
    {
        if(CurrntTimer != null)
        {
            StopCoroutine(CurrntTimer);
        }
        photonView.RPC("ReceiveSetDealFromTopRPC", RpcTarget.All, fromTop);
    }

    [PunRPC]
    void ReceiveSetDealFromTopRPC(bool fromTop)
    {
        StartCoroutine(StartDealCoroutine(fromTop));
    }

    IEnumerator StartDealCoroutine(bool fromTop)
    {
        OrderSelecrtionPanel.SetActive(false);
        if(masterClientTag == "Dealer")
        {
            StatusPanel.SetActive(true);
            statusText.text = fromTop ? "Click card pack to deal from Top" : "Click card pack to deal from Bottom";
        }
        yield return new WaitForSeconds(1f);         
        StatusPanel.SetActive(false);
        //fadePanel.SetActive(true);
        //yield return new WaitForSeconds(1f); 
        
        dealFromTop = fromTop;     
        

        if(masterClientTag == "Dealer")
        {
            CreateFullPack();
        }
    }


    /// <summary>
    /// Executes the action when a Card is clicked.
    /// </summary>
    /// <param name="cardScript">The Card script instance that was clicked.</param>
    public void OnTrumpCardClicked(Card cardScript)
    {
        if(trumpSelected)
        {
            Debug.Log("Trump has already been selected. Cannot select again.");
            return;
        }

        // Check if the clicked card belongs to the 'Selection' context (based on its parent)
        if (cardScript.transform.parent != null && cardScript.transform.parent.gameObject == Selection)
        {
            

            SendTrumpSelectedRPC(cardScript.CardValue); 

            
        }
        else
        {
            // Normal game card logic
            Debug.Log($"Card Played: {cardScript.CardValue} of {cardScript.CardSymbol}");
        }

       
    }


    void AutomaticSetTrumpCard()
    {

        int randomIndex = UnityEngine.Random.Range(0, cardValues.Count);
        string value = "2";
        value = cardValues[randomIndex];

        SendTrumpSelectedRPC(value);          
    }


    void SendTrumpSelectedRPC(string value)
    {
            
        photonView.RPC("ReceiveTrumpSelectedRPC", RpcTarget.All, value);
    }


    [PunRPC]
    void ReceiveTrumpSelectedRPC(string value)
    {   
        StatusPanel.SetActive(false);
        Selection.SetActive(false);  
        
        SelectTrumpValue(value);      

        if(masterClientTag == "Dealer")
        {
            StatusPanel.SetActive(true);
            statusText.text = "Waiting for Opponent Select Order";
        }
        else
        {

            OrderSelecrtionPanel.SetActive(true); 

            if(CurrntTimer != null)
            {
                StopCoroutine(CurrntTimer);
            }
            CurrntTimer = StartCoroutine(TimerCoroutine(15f, 5f, AutomaticSetDealFromTop));
        }

    }



    

// --- Updated OnPackCardClicked ---
public void OnPackCardClicked(Card cardScript)
{
    
    // Safety check for game state
    if (gameOver || !trumpSelected || isDealing)
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
}

public void DealButton()
{
    StartCoroutine(WaitAndAutoDeal());
}


IEnumerator WaitAndAutoDeal()
{
    yield return new WaitForSeconds(5f);
    DealPackCard();
}


public void DealPackCard()
{

    if (gameOver || !trumpSelected || isDealing)
    {
        Debug.Log("Game is over or trump not selected. No more cards can be dealt.");
        return;
    }

    if (cardIndex >= 0 && cardIndex < Pack.transform.childCount)
    {

        int targetIndex = dealFromTop? cardIndex : Pack.transform.childCount -1 - cardIndex;
        // 2. Get the child at the specific index
        Transform childTransform = Pack.transform.GetChild(targetIndex);

        // 3. Try to get the Card component and assign it to your variable
        Card cardTemp = childTransform.GetComponent<Card>();

        if (PhotonNetwork.InRoom)
        {
            // Send the card's unique name to all clients (including the local client via RpcTarget.All)
            photonView.RPC("SynchronizeCardDrawRPC", RpcTarget.All, cardTemp.name);
        }

        
    }
    else
    {
        Debug.LogError("Card index is out of range.");
    }


    cardIndex ++;
    
}



// --- New RPC Method for Dealing Cards ---

/// <summary>
/// RPC method called by the player who drew the card to synchronize the draw 
/// across all clients.
/// </summary>
/// <param name="cardName">The unique name of the card that was drawn (e.g., "King of Hearts").</param>
[PunRPC]
public void SynchronizeCardDrawRPC(string cardName)
{
    // Find the card object locally using its unique name.
    Card cardToDraw = Pack.transform.Find(cardName)?.GetComponent<Card>();
    
    if (cardToDraw != null)
    {
        // Execute the synchronized logic on all clients.
        ExecuteCardDraw(cardToDraw);
    }
    else
    {
        Debug.LogError($"SynchronizeCardDrawRPC: Could not find card named '{cardName}' in the Pack.");
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
        
        if(masterClientTag == "Dealer")
            {
                DealerAnimator.SetTrigger("left");
            }
        else
            {
                DealerAnimator.SetTrigger("right");
            }
        MoveCardWithDelay(cardScript, DisCards.transform, Card.CardType.Discard, 0.5f);
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
            DealerAnimator.SetTrigger("me");
        }
        else
        {
            targetHand = Hand_P2;
            newType = Card.CardType.Hand_P2;
            DealerAnimator.SetTrigger("you");
        }

        // 1. Move the card to the target hand
        MoveCardWithDelay(cardScript, targetHand.transform, newType, 0.5f);
        
        Debug.Log($"{cardScript.name} dealt to {(isPlayerOneTurn ? "Player 1" : "Player 2")}'s Hand.");
        gameState = GameState.PLAYING;

        // 2. Check for the Win condition *before* toggling the turn
        if(cardScript.CardValue == currentTrumpValue)
        {
           

            if((masterClientTag == "Dealer" && isPlayerOneTurn) || (masterClientTag != "Dealer" && !isPlayerOneTurn))
            {
                
                gameState = GameState.WIN;
            }
            else
            {
                
                gameState = GameState.LOSE;
            }

            
            gameOver = true;
            
            if(Pack.transform.childCount == 0)
            {
                DealerAnimator.SetBool("Deal", false);
            }
            
            //Pack.SetActive(false);
            
            
        }

        

        // 3. Toggle the turn flag for the next card (happens on ALL clients)
        isPlayerOneTurn = !isPlayerOneTurn;
    }
    
    // 4. Increment the turn ID (happens on ALL clients)
    turnId_++;

    StartCoroutine(WaitAndAutoDeal());

    // 5. Update Visuals
    //cardScript.UpdateCardVisual();

    Debug.Log($"State Sync: Turn ID is now {turnId_}. Next turn: {(isPlayerOneTurn ? "Player 1" : "Player 2")}");
}





/// <summary>
/// Starts the delayed movement process.
/// </summary>
public void MoveCardWithDelay(Card card, Transform newParent, Card.CardType newType, float delay)
{
    StartCoroutine(MoveCardCoroutine(card, newParent, newType, delay));
}

private IEnumerator MoveCardCoroutine(Card card, Transform newParent, Card.CardType newType, float delay)
{
    isDealing = true;
    // Perform the actual movement
    if (card != null) // Safety check in case card was destroyed during delay
    {
        card.transform.SetParent(newParent);
        card.cardtype = newType;
        
        // Update visuals immediately after parent change
        card.UpdateCardVisual();

        card.gameObject.SetActive(false); // Ensure the card is active after moving
        
        Debug.Log($"Delayed Move Complete: {card.name} moved to {newParent.name}");
    }


    DealingCard.SetActive(dealFromTop);
     // Wait for a short moment before starting the move
    yield return new WaitForSeconds(delay * 0.3f);
    DealingCard.SetActive(true);

    yield return new WaitForSeconds(delay * 0.4f);
    DealingCard.SetActive(false);
     // Wait for the specified time
    yield return new WaitForSeconds(delay * 0.3f);

    card.gameObject.SetActive(true); // Activate the card after the delay
    if(gameState == GameState.WIN || gameState == GameState.LOSE)
        {
        GameOver();
        }

    isDealing = false;
   
    


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

            CurrentValuePanel.SetActive(true);
            CurrentValueText.text = $"{currentTrumpValue}";
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
        //InstantiatePackFromData(shuffledDeck);
        
        //Debug.Log($"Created a full deck of {shuffledDeck.Count} cards in a **randomized** order in the '{Pack.name}' object.");

        // 5. Send the shuffled deck data to the other player
        photonView.RPC("ReceiveShuffledPackRPC", RpcTarget.All, symbols, values);
        Debug.Log("Sent shuffled deck data to opponent via RPC.");
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

        Pack.SetActive(true);
        DealerAnimator.SetBool("Deal", true);
        Hand_P1.transform.parent.gameObject.SetActive(true);
        Hand_P2.transform.parent.gameObject.SetActive(true);
        foreach (GameObject obj in MyObjects)
        {
            obj.SetActive(true);
        }

        if(!dealFromTop)
        {
            FlipPack();
        }

        if(AutoDeal)
        {
            DealPanel.SetActive(true);
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


    public void FlipPack()
    {
        Pack.GetComponent<WorldSpaceGridLayout>().FlipOrder();
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
        if (gameState == GameState.WIN || gameState == GameState.LOSE)
        {
            if(PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
            
        }else if (PhotonNetwork.PlayerList.Length == 1)
        {
            gameState = GameState.WIN;
            GameOver();
        }
    }

    public void GameOver()
    {
        if(PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
        
        winText.SetActive(true);
        if (gameState == GameState.WIN)
        {
            winText.GetComponent<TextMeshProUGUI>().text = "You Win!";

            if(CurrntTimer != null)
            {
                StopCoroutine(CurrntTimer);
            }
            CurrntTimer = StartCoroutine(TimerCoroutine(8f, 5f, showWin));
           
            
        }
        else if (gameState == GameState.LOSE)
        {
            winText.GetComponent<TextMeshProUGUI>().text = "You Lose!";

            if(CurrntTimer != null)
            {
                StopCoroutine(CurrntTimer);
            }
            CurrntTimer = StartCoroutine(TimerCoroutine(8f, 5f, showLoss));
        }
    }

    public void showWin()
    {
        WinVid.SetActive(true);
    }

    public void showLoss()
    {
        LossVid.SetActive(true);
    }

    public void CheckTurn()
    {
        
    }

    public void GoHome()
    {
        
        SceneManager.LoadScene("Menu");
    }

    IEnumerator TimerCoroutine(float duration, float countdown, Action onTimerComplete)
    {
        
        
        float remainingTime = duration;

        while (remainingTime > 0)
        {
            // 1. Update Fill Amount (0.0 to 1.0)
            

            // 2. Update Text only during the last 5 seconds
            if (TimeText != null)
            {
                if (remainingTime <= countdown)
                {
                    TimerPanel.SetActive(true);
                    // Ceiling gives us 5, 4, 3, 2, 1
                    TimeText.text = Mathf.CeilToInt(remainingTime).ToString();

                    if (Clock != null)
                    {
                        Clock.fillAmount = remainingTime/ countdown;
                    }
                }
                else
                {
                    TimeText.text = ""; // Keep empty until the 5s mark
                }
            }

            yield return null;
            remainingTime -= Time.deltaTime;
        }

        // Cleanup at the end
        if (Clock != null) Clock.fillAmount = 0;
        if (TimeText != null) TimeText.text = "0";

        TimerPanel.SetActive(false);

        onTimerComplete?.Invoke();
    }
}
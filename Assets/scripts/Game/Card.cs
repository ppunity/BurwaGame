using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))] 
public class Card : MonoBehaviour
{
    // --- Card Attributes ---
    
    [Header("Card Identity")]
    
    // We hide the fields so the custom editor can draw them as dropdowns instead of text inputs.
    [HideInInspector]
    [SerializeField] private string cardSymbol = "";
    public string CardSymbol => cardSymbol;

    [HideInInspector]
    [SerializeField] private string cardValue = "";
    public string CardValue => cardValue;

    public CardType cardtype ;

    [SerializeField] private TextMeshPro cardTextMesh;

    // --- Input Delegation ---

    public enum CardType
    {
        Selection, // Cards used for choosing trump
        Pack,      // The main draw deck
        Hand_P1,   // Player 1's hand
        Hand_P2,   // Player 2's hand
        Discard, 
        None,  // Discard pile/Played area
        // You can add more types like Table, Burn, etc. later
    }

    [SerializeField] private Sprite cardbackSprite;
    [SerializeField] private Sprite cardfrontSprite;

    private void OnMouseDown()
    {
        Debug.Log($"Card: OnMouseDown detected on {cardSymbol} {cardValue} of type {cardtype}");
        if (CardManager.Instance != null)
        {
            if(cardtype == CardType.Selection)
            {
                if(CardManager.Instance.masterClientTag == "NoneDealer")
                {
                    CardManager.Instance.OnTrumpCardClicked(this);
                }
                else
                {
                    Debug.Log("Only the NoneDealer can select the trump card.");
                }
                
            }
            else if(cardtype == CardType.Pack && !CardManager.Instance.AutoDeal)
            {
                if(CardManager.Instance.masterClientTag == "Dealer")
                {
                    CardManager.Instance.OnPackCardClicked(this);
                }
                else
                {
                    Debug.Log("Only the Dealer can draw from the pack.");
                }
            }
            else if(cardtype == CardType.Hand_P1 || cardtype == CardType.Hand_P2)
            {
                //CardManager.Instance.OnHandCardClicked(this);
            }
            else
            {
                Debug.Log($"Card: Card clicked - {cardSymbol} {cardValue}");
            }
            
        }
        else
        {
            Debug.LogError("CardManager.Instance is null! Cannot process card click.");
        }
    }

    // This method is primarily used by the custom editor script to set the values.
    // It's public so the editor script can access it.
    public void SetAttributes(string newSymbol, string newValue, CardType newType)
    {
        cardSymbol = newSymbol;
        cardValue = newValue;
        cardtype = newType;
    }

    public void UpdateCardVisual()
    {
        string displayText = "";
        string CardSymbolTmp = cardSymbol;
            if(cardSymbol == "Hearts")
            { 
                CardSymbolTmp = "♥";
                cardTextMesh.color = Color.red;
            }
            else if(cardSymbol == "Diamonds")
            {
                 CardSymbolTmp = "♦";
                 cardTextMesh.color = Color.red;
            }
            else if(cardSymbol == "Clubs")
            {
                CardSymbolTmp = "♣";
                cardTextMesh.color = Color.black;
            }
            else if(cardSymbol == "Spades")
            {
                 CardSymbolTmp = "♠";
                 cardTextMesh.color = Color.black;
            }
            else if(cardSymbol == "None")
            {
                 CardSymbolTmp = "";
            }
            displayText = $"{CardSymbolTmp}{cardValue}";


        if (cardTextMesh != null)
        {
            cardTextMesh.text = displayText;
        }
        else
        {
            Debug.LogWarning("Card: TextMeshPro component is not assigned.");
        }

        if (cardtype == CardType.Pack || cardtype == CardType.Discard && cardbackSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = cardbackSprite;
            }

            cardTextMesh.text = "";
        }
        else if((cardtype == CardType.Selection || cardtype == CardType.Hand_P1 || cardtype == CardType.Hand_P2) && cardbackSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = cardfrontSprite;
            }
            cardTextMesh.text = displayText;
        }

    }
}
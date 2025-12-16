using UnityEngine;
using UnityEditor;

// Tells Unity that this script is the custom editor for the Card class.
[CustomEditor(typeof(Card))]
public class CardEditor : Editor
{
    // --- Hardcoded Lists for Editor Mode (Self-Contained Data) ---
    // These lists are used ONLY for the Inspector dropdowns.
    // They should match the lists defined in your CardManager for consistency.
    private readonly string[] symbolOptions = new string[] { "Hearts", "Diamonds", "Clubs", "Spades", "None" };
    private readonly string[] valueOptions = new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A", "None" };

    // The main drawing method that replaces the default inspector.
    public override void OnInspectorGUI()
    {
        // Draw the default inspector properties (e.g., Header, Collider, etc.)
        DrawDefaultInspector();

        // Get the target Card instance we are editing
        Card cardTarget = (Card)target;
        
        // --- 1. Symbol Dropdown (for Trump Selection or regular Card Suit) ---
        
        EditorGUILayout.Space();
        
        // Find the index of the currently set symbol value in our local options list.
        int currentSymbolIndex = System.Array.IndexOf(symbolOptions, cardTarget.CardSymbol);
        
        // Ensure index is valid (or default to 0 if the value wasn't found)
        if (currentSymbolIndex < 0) currentSymbolIndex = 0;

        // Display the dropdown (Popup) in the Inspector
        int newSymbolIndex = EditorGUILayout.Popup("Card Symbol", currentSymbolIndex, symbolOptions);
        
        // --- 2. Value Dropdown (for regular Card Value) ---
        
        // Find the index of the currently set value in our local options list.
        int currentValueIndex = System.Array.IndexOf(valueOptions, cardTarget.CardValue);
        
        // Ensure index is valid (or default to 0 if the value wasn't found)
        if (currentValueIndex < 0) currentValueIndex = 0;
        
        int newValueIndex = EditorGUILayout.Popup("Card Value", currentValueIndex, valueOptions);


        // --- Apply Changes if Indices are Different ---
        
        if (newSymbolIndex != currentSymbolIndex || newValueIndex != currentValueIndex)
        {
            // Register an undo operation
            Undo.RecordObject(cardTarget, "Change Card Attributes");

            // Get the new symbol and value strings from the selected indices
            string selectedSymbol = symbolOptions[newSymbolIndex];
            string selectedValue = valueOptions[newValueIndex];

            // Use the public setter method on the Card script
            cardTarget.SetAttributes(selectedSymbol, selectedValue ,Card.CardType.None);
            
            // Mark the object as dirty so Unity saves the changes
            EditorUtility.SetDirty(cardTarget);
        }
    }
}
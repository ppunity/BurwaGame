using UnityEngine;

public class WorldSpaceGridLayout : MonoBehaviour
{
    // --- Configuration Variables ---

    [Header("Layout Settings")]
    public Vector2 cellSize = new Vector2(1.0f, 1.0f); 
    public Vector2 spacing = new Vector2(0.2f, 0.2f);
    public int columns = 5; 
    
    [Space]
    [Tooltip("The Z-depth gap added for each subsequent row.")]
    public float zSpacing = 0.0f;

    [Header("Offset & Anchor")]
    public Vector2 padding = Vector2.zero;
    public bool centerGrid = true;
    
    [Header("Runtime/Editor Refresh")]
    [Tooltip("Force refresh every frame. Disable for performance if children are only modified once.")]
    public bool forceRefresh = false;

    // Internal state to track changes
    private int previousChildCount = 0;
    private bool layoutNeedsUpdate = false;


    private void Start()
    {
        // Initial arrangement when the scene starts
        ArrangeChildren();
        previousChildCount = transform.childCount;
    }

    private void LateUpdate()
    {
        // 1. Check if the child count has changed (a simple and common case)
        if (transform.childCount != previousChildCount)
        {
            layoutNeedsUpdate = true;
            previousChildCount = transform.childCount;
        }

        // 2. Check the forceRefresh flag (useful for constantly moving/changing parents)
        if (forceRefresh)
        {
            layoutNeedsUpdate = true;
        }

        // Execute the arrangement if a flag was set
        if (layoutNeedsUpdate)
        {
            ArrangeChildren();
            layoutNeedsUpdate = false;
        }
    }
    
    /// <summary>
    /// Unity message called when the list of children changes (e.g., added or removed).
    /// This is the most efficient trigger for layout changes at runtime.
    /// </summary>
    private void OnTransformChildrenChanged()
    {
        // Set the flag to true so LateUpdate processes the layout change
        layoutNeedsUpdate = true;
    }

    // --- Core Layout Logic ---

    [ContextMenu("Arrange Children")]
    public void ArrangeChildren()
    {
        int childCount = transform.childCount;
        if (childCount == 0)
        {
            return;
        }

        // --- Step 1 & 2: Calculate Dimensions and Starting Offset ---
        
        float cellWidth = cellSize.x + spacing.x;
        float cellHeight = cellSize.y + spacing.y;
        int rows = Mathf.CeilToInt((float)childCount / columns);
        float gridWidth = (columns * cellWidth) - spacing.x;
        float gridHeight = (rows * cellHeight) - spacing.y;

        Vector3 startOffset = Vector3.zero;

        if (centerGrid)
        {
            startOffset.x -= gridWidth / 2f;
            startOffset.y += gridHeight / 2f;
        }

        startOffset.x += padding.x;
        startOffset.y -= padding.y;


        // --- Step 3: Position each child object ---

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);

            int column = i % columns;
            int row = i / columns;

            float xPos = column * cellWidth + (cellSize.x / 2f);
            float yPos = -(row * cellHeight) - (cellSize.y / 2f);
            float zPos = row * zSpacing; 

            child.localPosition = startOffset + new Vector3(xPos, yPos, zPos);
        }
    }

    // --- Editor Mode Refresh (Unchanged from previous versions) ---

    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Only run if Unity is not playing (for editor changes)
        if (!Application.isPlaying)
        {
            ArrangeChildren();
        }
    }
    #endif
}
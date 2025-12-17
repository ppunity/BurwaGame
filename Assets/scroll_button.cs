using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class scroll_button : MonoBehaviour
{
    [Header("UI References")]
    public ScrollRect scrollRect;
    public Button leftButton;
    public Button rightButton;
    public RectTransform content;
    public RectTransform[] items;

    [Header("Scaling Settings")]
    public float focusedScale = 1.0f;
    public float unfocusedScale = 0.7f;
    public float scaleSmooth = 10f;

    [Header("Scroll Settings")]
    public float scrollSmooth = 10f;

    [Header("Focus Point Settings")]
    [Tooltip("Normalized 0 = left, 0.5 = center, 1 = right of viewport")]
    [Range(0f, 1f)]
    public float focusPoint = 0.5f;

    [Header("Snap Settings")]
    [Tooltip("Enable automatic snapping to nearest item")]
    public bool enableSnap = true;
    [Tooltip("Velocity threshold below which snapping begins")]
    public float snapVelocityThreshold = 50f;
    [Tooltip("Speed of snapping animation")]
    public float snapSpeed = 8f;

    private int currentIndex = 0;
    private RectTransform viewport;
    private bool isSnapping = false;
    private bool isDragging = false;
    private float targetNormalizedPos = 0f;

    private void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (content == null && scrollRect != null) content = scrollRect.content;
    }

    private void Start()
    {
        viewport = (scrollRect != null && scrollRect.viewport != null)
            ? scrollRect.viewport
            : (RectTransform)scrollRect.transform;

        if (leftButton) leftButton.onClick.AddListener(ScrollLeft);
        if (rightButton) rightButton.onClick.AddListener(ScrollRight);

        // Add drag detection listeners
        AddDragListeners();

        ScrollToIndex(0);
    }

    private void Update()
    {
        UpdateItemScales();
        HandleSnapping();
    }

    private void AddDragListeners()
    {
        // Create event trigger component if it doesn't exist
        EventTrigger trigger = scrollRect.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = scrollRect.gameObject.AddComponent<EventTrigger>();
        }

        // Begin drag
        EventTrigger.Entry beginDrag = new EventTrigger.Entry();
        beginDrag.eventID = EventTriggerType.BeginDrag;
        beginDrag.callback.AddListener((data) => { OnBeginDrag(); });
        trigger.triggers.Add(beginDrag);

        // End drag
        EventTrigger.Entry endDrag = new EventTrigger.Entry();
        endDrag.eventID = EventTriggerType.EndDrag;
        endDrag.callback.AddListener((data) => { OnEndDrag(); });
        trigger.triggers.Add(endDrag);
    }

    private void OnBeginDrag()
    {
        isDragging = true;
        isSnapping = false;
    }

    private void OnEndDrag()
    {
        isDragging = false;
    }

    private void HandleSnapping()
    {
        if (!enableSnap || items == null || items.Length == 0) return;

        // Check if we should start snapping
        if (!isDragging && !isSnapping)
        {
            // Check velocity
            float velocity = Mathf.Abs(scrollRect.velocity.x);
            
            if (velocity < snapVelocityThreshold)
            {
                // Find nearest item and start snapping
                int nearestIndex = FindNearestItemToFocusPoint();
                if (nearestIndex != -1)
                {
                    currentIndex = nearestIndex;
                    isSnapping = true;
                    targetNormalizedPos = CalculateNormalizedPositionForIndex(nearestIndex);
                }
            }
        }

        // Perform snapping animation
        if (isSnapping)
        {
            float currentPos = scrollRect.horizontalNormalizedPosition;
            float newPos = Mathf.Lerp(currentPos, targetNormalizedPos, Time.deltaTime * snapSpeed);
            
            scrollRect.horizontalNormalizedPosition = newPos;

            // Stop snapping when close enough
            if (Mathf.Abs(newPos - targetNormalizedPos) < 0.001f)
            {
                scrollRect.horizontalNormalizedPosition = targetNormalizedPos;
                isSnapping = false;
                scrollRect.velocity = Vector2.zero; // Stop any residual movement
            }
        }
    }

    private int FindNearestItemToFocusPoint()
    {
        if (viewport == null) return -1;

        float focusLocalX = (focusPoint - 0.5f) * viewport.rect.width;
        Vector3 focusLocal = new Vector3(focusLocalX, 0f, 0f);

        int nearestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;

            Vector3 itemLocal = viewport.InverseTransformPoint(items[i].position);
            float distance = Mathf.Abs(focusLocal.x - itemLocal.x);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private float CalculateNormalizedPositionForIndex(int index)
    {
        if (items.Length <= 1) return 0f;

        // Calculate the position needed to center the item at focusPoint
        if (viewport == null || content == null) 
            return (float)index / (items.Length - 1);

        RectTransform item = items[index];
        if (item == null) return (float)index / (items.Length - 1);

        // Get the item's position in content space
        float itemPosX = item.localPosition.x;
        
        // Calculate the content width and viewport width
        float contentWidth = content.rect.width;
        float viewportWidth = viewport.rect.width;
        
        // Calculate the scroll range
        float scrollRange = contentWidth - viewportWidth;
        if (scrollRange <= 0) return 0f;

        // Calculate offset to align item with focus point
        float focusOffset = (focusPoint - 0.5f) * viewportWidth;
        float targetContentX = -itemPosX + focusOffset;

        // Convert to normalized position (0 to 1)
        float normalizedPos = Mathf.Clamp01(-targetContentX / scrollRange);

        foreach (RectTransform itm in items)
        {
            if (itm == null) continue;

            Button[] buttons = itm.gameObject.GetComponentsInChildren<Button>();

            // Iterate through the found buttons and do something with them
            foreach (Button button in buttons)
            {
                button.interactable = (itm == item);
            }
        }
        
        
        
        return normalizedPos;
    }

    private void UpdateItemScales()
    {
        if (items == null || items.Length == 0 || viewport == null) return;

        float focusLocalX = (focusPoint - 0.5f) * viewport.rect.width;
        Vector3 focusLocal = new Vector3(focusLocalX, 0f, 0f);

        for (int i = 0; i < items.Length; i++)
        {
            RectTransform item = items[i];
            if (item == null) continue;

            Vector3 itemLocal = viewport.InverseTransformPoint(item.position);
            float distance = Mathf.Abs(focusLocal.x - itemLocal.x);

            float t = Mathf.Clamp01(distance / (viewport.rect.width * 0.5f));
            float targetScale = Mathf.Lerp(focusedScale, unfocusedScale, t);

            item.localScale = Vector3.Lerp(
                item.localScale,
                Vector3.one * targetScale,
                Time.deltaTime * scaleSmooth
            );
        }
    }

    private void ScrollLeft()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ScrollToIndex(currentIndex);
        }
    }

    private void ScrollRight()
    {
        if (currentIndex < items.Length - 1)
        {
            currentIndex++;
            ScrollToIndex(currentIndex);
        }
    }

    private void ScrollToIndex(int index)
    {
        if (items == null || items.Length == 0) return;

        currentIndex = Mathf.Clamp(index, 0, items.Length - 1);
        
        isSnapping = true;
        isDragging = false;
        targetNormalizedPos = CalculateNormalizedPositionForIndex(currentIndex);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (content != null && (items == null || items.Length == 0))
        {
            int n = content.childCount;
            RectTransform[] arr = new RectTransform[n];
            for (int i = 0; i < n; i++) arr[i] = content.GetChild(i) as RectTransform;
            items = arr;
        }
    }
#endif
}
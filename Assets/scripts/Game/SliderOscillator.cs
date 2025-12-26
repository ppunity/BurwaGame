using UnityEngine;
using UnityEngine.UI;

public class SliderOscillator : MonoBehaviour
{
    [Header("Settings")]
    public Slider targetSlider;
    public float speed = 1.0f;

    void Start()
    {
        // If not assigned in inspector, try to find it on this GameObject
        if (targetSlider == null)
        {
            targetSlider = GetComponent<Slider>();
        }
    }

    void Update()
    {
        if (targetSlider != null)
        {
            // Mathf.PingPong(time, length) moves from 0 to length, then back to 0
            targetSlider.value = Mathf.PingPong(Time.time * speed, 1.0f);
        }
    }
}
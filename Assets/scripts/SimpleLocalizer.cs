using UnityEngine;

public class SimpleLocalizer : MonoBehaviour
{
    void Start()
    {
        
    }

    void OnEnable()
    {
        UpdateLanguage();
    }

    // Call this if the player changes language while the game is running
    public void UpdateLanguage()
    {
        // Get the saved language index, default to 0 if it doesn't exist
        int langIndex = PlayerPrefs.GetInt("lanbp", 0);

        // Loop through all children of this object
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;

            // Activate child if its index matches lanbp, otherwise deactivate
            if (i == langIndex)
            {
                child.SetActive(true);
            }
            else
            {
                child.SetActive(false);
            }
        }
    }

    public void CycleLanguage()
    {
        int currentLan = PlayerPrefs.GetInt("lanbp", 0);

        int nextLan = (currentLan + 1) % 3;

        PlayerPrefs.SetInt("lanbp", nextLan);
        PlayerPrefs.Save();
    }
}
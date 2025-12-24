using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Networking;
using System.Linq;




public class PlayportUIUpdater : MonoBehaviour
{
    [Header("Auto-Update Settings")]
    [SerializeField] private bool autoUpdateOnEnable = true;
    [SerializeField] private float updateInterval = 1f; // Check for updates every second

    [Header("UI Elements to Update")]
    [SerializeField] private TextMeshProUGUI usernameTextTMP;
    [SerializeField] private Text usernameText;
    [SerializeField] private TextMeshProUGUI coinsTextTMP;
    [SerializeField] private Text coinsText;
    [SerializeField] private Image profileImage;

    [Header("Display Format")]
    [SerializeField] private string usernamePrefix = "";
    [SerializeField] private string usernameSuffix = "";
    [SerializeField] private string coinsPrefix = "";
    [SerializeField] private string coinsSuffix = "";

    private Coroutine updateCoroutine;
    private string lastUsername = "";
    private int lastCoins = -1;

    void OnEnable()
    {
        PlayportDataHelper.ProfileUpdate();

        if (autoUpdateOnEnable)
        {
            UpdateUI();

            // Start periodic updates
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            updateCoroutine = StartCoroutine(PeriodicUpdate());
        }
    }

    void OnDisable()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }

    /// <summary>
    /// Periodically check for data updates
    /// </summary>
    private IEnumerator PeriodicUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            // Only update if data has changed
            string currentUsername = PlayportDataHelper.GetUsername();
            int currentCoins = PlayportDataHelper.GetCoins();

            if (currentUsername != lastUsername || currentCoins != lastCoins)
            {
                UpdateUI();
            }
        }
    }

    /// <summary>
    /// Manually update all UI elements
    /// </summary>
    public void UpdateUI()
    {
        UpdateUsername();
        UpdateCoins();
        UpdateProfileImage();
    }

    /// <summary>
    /// Update username text
    /// </summary>
    public void UpdateUsername()
    {
        string username = PlayportDataHelper.GetUsername("Guest");
        lastUsername = username;

        string displayText = usernamePrefix + LimitNameText(username) + usernameSuffix;

        if (usernameTextTMP != null)
        {
            usernameTextTMP.text = displayText;
        }

        if (usernameText != null)
        {
            usernameText.text = displayText;
        }
    }

    private string LimitNameText(string fullName, int maxLength = 8)
    {
        if (string.IsNullOrEmpty(fullName)) return "";
        return fullName.Length > maxLength ? fullName.Substring(0, maxLength) + ".." : fullName;
    }

    /// <summary>
    /// Update coins text
    /// </summary>
    public void UpdateCoins()
    {
        int coins = PlayportDataHelper.GetCoins(0);
        lastCoins = coins;
        string coinString = coins.ToString();
        if (coins == 0)
        {
            coinString = "0";


        }
        else
        {
            //coinString = FormatCurrency(coins);
            coinString = coins.ToString("N0");
        }
            
        string displayText = coinsPrefix + coinString + coinsSuffix;
        

        if (coinsTextTMP != null)
        {           
            coinsTextTMP.text = displayText;
        }

        if (coinsText != null)
        {
            coinsText.text = displayText;
        }
    }

private string FormatCurrency(int amount)
    {
        if (amount >= 1000000)
        {
            // For millions: 1.5M, 2M, etc.
            return (amount / 1000000f).ToString("0.#") + "M";
        }
        else if (amount >= 1000)
        {
            // For thousands: 1k, 1.2k, 15k, etc.
            return (amount / 1000f).ToString("0.#") + "k";
        }
        else
        {
            // Below 1000: show as-is
            return amount.ToString();
        }
    }

    /// <summary>
    /// Update profile image from Playport data
    /// </summary>
    public void UpdateProfileImage()
    {
        if (profileImage != null && playport.Instance != null)
        {
            

            if (playport.Instance.ProfileSprite != null)
            {
                profileImage.sprite = playport.Instance.ProfileSprite;
            }
            else
            {
                profileImage.sprite = playport.Instance.defaultProfileSprite;
            }
        }
    }

    

    private IEnumerator DownloadAndSetImage(string imageUrl)
    {
        using (UnityEngine.Networking.UnityWebRequest request =
               UnityEngine.Networking.UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                if (profileImage != null)
                {
                    profileImage.sprite = sprite;
                }
            }
        }
    }
}
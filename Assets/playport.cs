using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Networking;
using System.Linq;
using System;

public class playport : MonoBehaviour
{
    // Singleton instance
    public static playport Instance { get; private set; }

    [Header("User Data")]
    public UserData MyUserData;
    [SerializeField] private UserData SampleUserData;

    [Header("UI Text Elements")]
    public TextMeshProUGUI idText;
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI phoneNumberText;
    public TextMeshProUGUI emailText;
    public TextMeshProUGUI totalCoinsText;
    public TextMeshProUGUI isActiveText;
    public TextMeshProUGUI isVerifiedText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI suspensionReasonText;
    public TextMeshProUGUI suspendedUntilText;
    public TextMeshProUGUI kycStatusText;
    public TextMeshProUGUI kycDocumentsText;
    public TextMeshProUGUI riskScoreText;
    public TextMeshProUGUI createdAtText;
    public TextMeshProUGUI updatedAtText;
    public TextMeshProUGUI lastLoginAtText;
    public TextMeshProUGUI suspendedByText;
    public TextMeshProUGUI gameIdText;

    [Header("UI Image Elements")]
    public Image profileImage;
    public Sprite ProfileSprite;
    public Sprite defaultProfileSprite;

    [Header("API Settings")]
    [SerializeField] private string apiUrl = "https://api.playport.lk/api/v1/users/profile";
    private string _gameId = "";
    private string _roomName = "";


    private Coroutine currentProfileRequest;

    public delegate void BetTransactionCallback(bool success, string message);

    [System.Serializable]
    public class BetRequest
    {
        public string game_id;
        public int bet_amount;
        public string result;// "debit" or "credit"
    }

    [System.Serializable]
    public class BetResponse
    {
        public bool success;
        public string message;
        // Add more fields if your API returns additional data
    }


    void Awake()
    {
        
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("playport set to DontDestroyOnLoad");
        }
        else
        {
            Destroy(gameObject);
            return;
        }


        InitializeGameID();
    }

    void Start()
    {
        
        GetUserProfile();
    }


    public void RefreshThis()
    {
        
        GetUserProfile();
    }

    #region Data Classes

    [System.Serializable]
    public class ApiResponse
    {
        public bool success;
        public string message;
        public UserData data;
    }

    [System.Serializable]
    public class UserData
    {
        public string id;
        public string username;
        public string phone_number;
        public string email;
        public string password_hash;
        public int total_coins;
        public string profile_image_url;
        public bool is_active;
        public bool is_verified;
        public string status;
        public string suspension_reason;
        public string suspended_until;
        public string kyc_status;
        public string kyc_documents;
        public int risk_score;
        public string created_at;
        public string updated_at;
        public string last_login_at;
        public string suspended_by;
        public string game_id;
    }

    [System.Serializable]
    public class UpdateProfileRequest
    {
        public int total_coins;
    }

    [System.Serializable]
    public class UpdateGameStatsRequest
    {
        public string game_id;
        public int wins;
        public int losses;
    }

    [System.Serializable]
    public class GameStatsResponse
    {
        public bool success;
        public string message;
    }

    [System.Serializable]
    public class BetTransactionRequest
    {
        public string game_id;
        public int bet_amount;
        public string result; // "debit" or "credit"
        public string round_id; // NEW
    }

    [System.Serializable]
    public class BetTransactionResponse
    {
        public bool success;
        public string message;
    }


    #endregion

    #region UI Update Methods

    /// <summary>
    /// Updates all UI elements with user data
    /// </summary>
    private void UpdateUI(UserData userData)
    {
        if (userData == null)
        {
            Debug.LogWarning("UserData is null, cannot update UI");
            return;
        }

        // Update text fields
        SafeSetText(idText, userData.id);
        SafeSetText(usernameText, userData.username);
        SafeSetText(phoneNumberText, userData.phone_number);
        SafeSetText(emailText, userData.email);
        SafeSetText(totalCoinsText, userData.total_coins.ToString());
        SafeSetText(isActiveText, userData.is_active ? "Active" : "Inactive");
        SafeSetText(isVerifiedText, userData.is_verified ? "Verified" : "Not Verified");
        SafeSetText(statusText, userData.status);
        SafeSetText(suspensionReasonText, string.IsNullOrEmpty(userData.suspension_reason) ? "None" : userData.suspension_reason);
        SafeSetText(suspendedUntilText, string.IsNullOrEmpty(userData.suspended_until) ? "N/A" : userData.suspended_until);
        SafeSetText(kycStatusText, userData.kyc_status);
        SafeSetText(kycDocumentsText, string.IsNullOrEmpty(userData.kyc_documents) ? "None" : userData.kyc_documents);
        SafeSetText(riskScoreText, userData.risk_score.ToString());
        SafeSetText(createdAtText, FormatDateTime(userData.created_at));
        SafeSetText(updatedAtText, FormatDateTime(userData.updated_at));
        SafeSetText(lastLoginAtText, FormatDateTime(userData.last_login_at));
        SafeSetText(suspendedByText, string.IsNullOrEmpty(userData.suspended_by) ? "N/A" : userData.suspended_by);
        SafeSetText(gameIdText, userData.game_id);

        // Download and set profile image
        if (!string.IsNullOrEmpty(userData.profile_image_url))
        {
            StartCoroutine(DownloadProfileImage(userData.profile_image_url));
        }
        else
        {
            SetDefaultProfileImage();
        }

        Debug.Log("UI updated successfully with user data");
    }

    /// <summary>
    /// Safely sets text to a TextMeshProUGUI component
    /// </summary>
    private void SafeSetText(TextMeshProUGUI textComponent, string value)
    {
        if (textComponent != null)
        {
            textComponent.text = value ?? "N/A";
        }
    }

    /// <summary>
    /// Formats datetime string for better readability
    /// </summary>
    private string FormatDateTime(string dateTimeStr)
    {
        if (string.IsNullOrEmpty(dateTimeStr))
            return "N/A";

        try
        {
            System.DateTime dt = System.DateTime.Parse(dateTimeStr);
            return dt.ToString("MMM dd, yyyy hh:mm tt");
        }
        catch
        {
            return dateTimeStr;
        }
    }

    public string GenerateRoomID()
        {
            string baseId = GAME_ID != null ? GAME_ID: System.Guid.NewGuid().ToString("N");

            string suffix = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            return $"{baseId}-{suffix}";
        }


    /// <summary>
    /// Downloads profile image from URL
    /// </summary>
    private IEnumerator DownloadProfileImage(string imageUrl)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();
            

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                ProfileSprite = sprite;

                if (profileImage != null)
                {
                    profileImage.sprite = sprite;
                    Debug.Log("Profile image downloaded successfully");
                }
            }
            else
            {
                Debug.LogError($"Failed to download profile image: {request.error}");
                SetDefaultProfileImage();
            }
        }
    }

    /// <summary>
    /// Sets default profile image
    /// </summary>
    private void SetDefaultProfileImage()
    {
        if (profileImage != null && defaultProfileSprite != null)
        {
            profileImage.sprite = defaultProfileSprite;
        }
    }

    #endregion

    #region API Methods

    public string GAME_ID
    {
        get
        {
            return _gameId;
        }
        set
        {
            _gameId = value;
        }
    }

    public string ROOM_NAME
    {
        get
        {
            return _roomName;
        }
        set
        {
            _roomName = value;
        }
    }

    public void Handle401Unauthorized()
    {
        Debug.LogWarning("[APIManager] 401 Unauthorized - Redirecting to login page");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            Application.ExternalEval("window.location.replace('https://playport.lk');");
        #else
            Application.OpenURL("https://playport.lk");
        #endif
    }

    // Helper method to check response code
    private bool CheckForUnauthorized(UnityWebRequest request)
    {
        if (request.responseCode == 401)
        {
            Handle401Unauthorized();
            return true;
        }
        return false;
    }

    public void SetRoomName(string roomName)
    {
        ROOM_NAME = roomName;
    }

    private void InitializeGameID()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string url = Application.absoluteURL;
        
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                int queryStart = url.IndexOf('?');
                if (queryStart >= 0)
                {
                    string queryString = url.Substring(queryStart + 1);
                    string[] parameters = queryString.Split('&');
                    
                    foreach (string param in parameters)
                    {
                        int equalIndex = param.IndexOf('=');
                        if (equalIndex > 0)
                        {
                            string key = param.Substring(0, equalIndex);
                            string value = param.Substring(equalIndex + 1);
                            
                            if (key == "gameid" || key == "gameId" || key == "game_id")
                            {
                                GAME_ID = UnityEngine.Networking.UnityWebRequest.UnEscapeURL(value);
                                Debug.Log($"[InitializeGameID] GAME_ID extracted from URL: {GAME_ID}");
                                break;
                            }
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(GAME_ID))
                {
                    Debug.LogWarning("[InitializeGameID] No gameid found in URL");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[InitializeGameID] Error parsing URL: " + e.Message);
            }
        }
        else
        {
            Debug.LogWarning("[InitializeGameID] Application.absoluteURL is empty");
        }
#else
        // For testing in editor
        GAME_ID = "ecb38eff-357a-40df-bdb1-c2e46b783256";
        Debug.Log($"[InitializeGameID] Using test GAME_ID in editor: {GAME_ID}");
#endif
    }
   
    public void GetUserProfile()
    {
        if (currentProfileRequest != null)
        {
            StopCoroutine(currentProfileRequest);
        }

        Debug.Log("Fetching user profile...");
        currentProfileRequest = StartCoroutine(GetUserProfileCoroutine());
    }
/*
public IEnumerator GetUserProfileCoroutine()
{
    // Add cache-busting timestamp to URL
    string url = $"{apiUrl}?_={DateTime.UtcNow.Ticks}";
    
    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
        // Add cache-prevention headers
        request.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        request.SetRequestHeader("Pragma", "no-cache");
        request.SetRequestHeader("Expires", "0");
        
        // Ensure proper disposal (important for WebGL)
        request.disposeCertificateHandlerOnDispose = true;
        request.disposeDownloadHandlerOnDispose = true;
        request.disposeUploadHandlerOnDispose = true;
        
        Debug.Log("Sending profile request...");

        yield return request.SendWebRequest(); 

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error fetching profile: {request.error}");
            
            // Use sample data on error
            MyUserData = SampleUserData;
            if (MyUserData != null)
            {
                UpdateUI(MyUserData);
            }
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log("Raw Response: " + jsonResponse);

            ApiResponse response = JsonUtility.FromJson<ApiResponse>(jsonResponse);

            if (response != null && response.success)
            {
                Debug.Log("Profile fetched successfully!");
                OnProfileFetched(response.data);
            }
            else
            {
                Debug.LogWarning("API returned success=false");
                MyUserData = SampleUserData;
                if (MyUserData != null)
                {
                    UpdateUI(MyUserData);
                }
            }
        }
    }
}*/


public IEnumerator GetUserProfileCoroutine()
{

    yield return new WaitForSeconds(0.5f);
    // -----------------------------------------------------
    // 1. Always extract TOKEN + GAME_ID from URL
    // -----------------------------------------------------
    string token = GetTokenFromURL();  // This will set GAME_ID internally

    if (string.IsNullOrEmpty(token))
    {
        Debug.LogError("[GetUserProfile] TOKEN missing from URL!");
        MyUserData = SampleUserData;
        UpdateUI(MyUserData);
        yield break;
    }

    if (string.IsNullOrEmpty(GAME_ID))
    {
        Debug.LogError("[GetUserProfile] GAME_ID missing from URL!");
        MyUserData = SampleUserData;
        UpdateUI(MyUserData);
        yield break;
    }

    // -----------------------------------------------------
    // 2. Build API URL with cache-busting
    // -----------------------------------------------------
    string url = $"{apiUrl}?_={DateTime.UtcNow.Ticks}";

    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
        // Attach Bearer token (important)
        request.SetRequestHeader("Authorization", "Bearer " + token);

        // Cache-prevention
        request.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        request.SetRequestHeader("Pragma", "no-cache");
        request.SetRequestHeader("Expires", "0");

        request.disposeCertificateHandlerOnDispose = true;
        request.disposeDownloadHandlerOnDispose = true;
        request.disposeUploadHandlerOnDispose = true;

        Debug.Log("[GetUserProfile] Sending profile request...");

        // -----------------------------------------------------
        // 3. Send request
        // -----------------------------------------------------
        yield return request.SendWebRequest();

        if (CheckForUnauthorized(request))
        {
            yield break;
        }

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"[GetUserProfile] Error: {request.error}");

            MyUserData = SampleUserData;
            UpdateUI(MyUserData);
            yield break;
        }

        // -----------------------------------------------------
        // 4. Read response
        // -----------------------------------------------------
        string jsonResponse = request.downloadHandler.text;
        Debug.Log("[GetUserProfile] Raw Response: " + jsonResponse);

        ApiResponse response = JsonUtility.FromJson<ApiResponse>(jsonResponse);

        if (response != null && response.success)
        {
            Debug.Log("[GetUserProfile] Profile fetched successfully!");
            OnProfileFetched(response.data);
        }
        else
        {
            Debug.LogWarning("[GetUserProfile] API returned success=false");
            MyUserData = SampleUserData;
            UpdateUI(MyUserData);
        }
    }
}



public string GetTokenFromURL()
{
    string url = "";

#if UNITY_EDITOR
    // MOCK URL FOR TESTING: Replace with a real valid GameID + Token string from your staging/dev environment
    url = "https://pool.playport.lk/?gameid=70b70bb1-362e-45fa-95f0-ac0d39f2c9f5eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiIwMWY1MWYzOS03MDdhLTQxYzktYWUxZi05NzQ5ZmJjYjE4ZTEiLCJyb2xlIjoidXNlciIsInNlc3Npb25JZCI6ImUxYzhiY2RmN2U1OGZlNmQ3NTJiYzJkZjM5NWEwYzdlIiwiaWF0IjoxNzY2NTU4NTU5fQ.oU4sBvUlyLjrt0Z88EClo1FE-Sm1aya2cospJt9i4Gw";
    Debug.Log("[EDITOR] Using Mock URL: " + url);
#else
    url = Application.absoluteURL;
#endif

    if (!string.IsNullOrEmpty(url))
    {
        try
        {
            int queryStart = url.IndexOf('?');
            if (queryStart >= 0)
            {
                string queryString = url.Substring(queryStart + 1);
                string[] parameters = queryString.Split('&');

                foreach (string param in parameters)
                {
                    int equalIndex = param.IndexOf('=');
                    if (equalIndex > 0)
                    {
                        string key = param.Substring(0, equalIndex);
                        string value = param.Substring(equalIndex + 1);

                        if (key == "gameid" || key == "gameId" || key == "game_id")
                        {
                            string raw = UnityWebRequest.UnEscapeURL(value);

                            if (raw.Length > 36)
                            {
                                GAME_ID = raw.Substring(0, 36);   // First 36 chars = GameID
                                string token = raw.Substring(36); // Rest = JWT

                                Debug.Log("GAME ID: " + GAME_ID);
                                Debug.Log("TOKEN: " + token);

                                return token;
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing URL: " + e.Message);
        }
    }

    return null;
}


private void OnProfileFetched(UserData userData)
{
    MyUserData = userData;
    Debug.Log("Profile fetched and stored.");

    // Update UI with fetched data
    UpdateUI(userData);

    
}
    public void UpdateWins()
    {
        StartCoroutine(UpdateGameStatsCoroutine(wins: 1, losses: 0));
    }

    public void UpdateLosses()
    {
        StartCoroutine(UpdateGameStatsCoroutine(wins: 0, losses: 1));
    }

    public IEnumerator UpdateGameStatsCoroutine(int wins, int losses)
{
    // ---------------------------------------
    // 1. Always get token & game_id from URL
    // ---------------------------------------
    string token = GetTokenFromURL(); // This ALSO sets GAME_ID inside the function

    if (string.IsNullOrEmpty(token))
    {
        Debug.LogError("[UpdateGameStats] TOKEN missing from URL!");
        yield break;
    }

    if (string.IsNullOrEmpty(GAME_ID))
    {
        Debug.LogError("[UpdateGameStats] GAME_ID missing from URL!");
        yield break;
    }

    // ---------------------------------------
    // 2. Build JSON payload
    // ---------------------------------------
    string json;

    if (wins > 0)
    {
        json = $"{{\"game_id\":\"{GAME_ID}\",\"wins\":{wins}}}";
    }
    else
    {
        json = $"{{\"game_id\":\"{GAME_ID}\",\"losses\":{losses}}}";
    }

    // ---------------------------------------
    // 3. Build request
    // ---------------------------------------
    using (var request = new UnityWebRequest("https://api.playport.lk/api/v1/games-stats", "POST"))
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        Debug.Log("[UpdateGameStats] Sending stats update...");

        // ---------------------------------------
        // 4. Send request
        // ---------------------------------------
        yield return request.SendWebRequest();

        if (CheckForUnauthorized(request))
        {
            yield break;
        }

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"[UpdateGameStats] Error: {request.error}");
            yield break;
        }

        string jsonResponse = request.downloadHandler.text;
        Debug.Log("[UpdateGameStats] Response: " + jsonResponse);

        GameStatsResponse response = JsonUtility.FromJson<GameStatsResponse>(jsonResponse);

        if (response != null && response.success)
        {
            Debug.Log($"[UpdateGameStats] {(wins > 0 ? "Win" : "Loss")} recorded!");
        }
        else
        {
            Debug.LogWarning("[UpdateGameStats] API returned success=false");
        }
    }
}

public void PostBetTransaction(int betAmount, string result, string roundId, BetTransactionCallback callback = null)
{
    StartCoroutine(PostBetTransactionCoroutine(betAmount, result, roundId, callback));
}

public IEnumerator PostBetTransactionCoroutine(int betAmount, string result, string roundId = null, BetTransactionCallback callback = null)
{
    // -----------------------------------------------------
    // 1. Always get token + game id from URL
    // -----------------------------------------------------
    string token = GetTokenFromURL();

    if (string.IsNullOrEmpty(token))
    {
        Debug.LogError("[PostBetTransaction] TOKEN missing from URL!");
        callback?.Invoke(false, "Token missing from URL");
        yield break;
    }

    if (string.IsNullOrEmpty(GAME_ID))
    {
        Debug.LogError("[PostBetTransaction] GAME_ID missing from URL!");
        callback?.Invoke(false, "GAME_ID missing from URL");
        yield break;
    }

    // -----------------------------------------------------
    // 2. Build request body
    // -----------------------------------------------------
    Debug.Log($"[PostBetTransaction] Sending bet transaction - Amount: {betAmount}, Result: {result}, GAME_ID: {GAME_ID}, round_id: {roundId}");

    var betRequest = new BetTransactionRequest
    {
        game_id = GAME_ID,
        bet_amount = betAmount,
        result = result,
        round_id = roundId
    };

    string json = JsonUtility.ToJson(betRequest);
    Debug.Log($"[PostBetTransaction] JSON: {json}");

    // -----------------------------------------------------
    // 3. Send POST request
    // -----------------------------------------------------
    using (var request = new UnityWebRequest("https://api.playport.lk/api/v1/transactions/bet", "POST"))
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        Debug.Log("[PostBetTransaction] Sending request...");

        yield return request.SendWebRequest();

        // -----------------------------------------------------
        // 4. Unauthorized handler
        // -----------------------------------------------------
        if (CheckForUnauthorized(request))
        {
            callback?.Invoke(false, "Unauthorized");
            yield break;
        }

        // -----------------------------------------------------
        // 5. Error handling
        // -----------------------------------------------------
        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"[PostBetTransaction] Error: {request.error}");
            Debug.LogError($"[PostBetTransaction] Response: {request.downloadHandler.text}");
            callback?.Invoke(false, request.error);
            yield break;
        }

        // -----------------------------------------------------
        // 6. Success handling
        // -----------------------------------------------------
        string jsonResponse = request.downloadHandler.text;
        Debug.Log("[PostBetTransaction] Response: " + jsonResponse);

        BetTransactionResponse response = JsonUtility.FromJson<BetTransactionResponse>(jsonResponse);

        if (response != null && response.success)
        {
            Debug.Log($"[PostBetTransaction] Success! Bet transaction recorded - {result}");
            callback?.Invoke(true, response.message);
        }
        else
        {
            Debug.LogWarning("[PostBetTransaction] Transaction failed or returned unsuccessful response");
            callback?.Invoke(false, response?.message ?? "Unknown error");
        }
    }
}




    #endregion

    #region Public Helper Methods

    /// <summary>
    /// Manually refresh the UI with current user data
    /// </summary>
    public void RefreshUI()
    {
        if (MyUserData != null)
        {
            UpdateUI(MyUserData);
        }
        else
        {
            GetUserProfile();
        }
    }

    /// <summary>
    /// Get current user data
    /// </summary>
    public UserData GetCurrentUserData()
    {
        return MyUserData;
    }

    #endregion
}

public static class PlayportDataHelper
{
    /// <summary>
    /// Get username from Playport API, returns fallback if not available
    /// </summary>
    public static string GetUsername(string fallback = "Guest")
    {
       var userData = playport.Instance.GetCurrentUserData();
       return userData.username;        
       
    }

    public static string GetProfileUrl(string fallback = "Guest")
    {
        var userData = playport.Instance.GetCurrentUserData();
       return userData.profile_image_url;  
    }

    /// <summary>
    /// Get coins count from Playport API, returns fallback if not available
    /// </summary>
    public static int GetCoins(int fallback = 0)
    {
        var userData = playport.Instance.GetCurrentUserData();
        return userData.total_coins;
        
    }

    /// <summary>
    /// Get user email, returns empty string if not available
    /// </summary>
    public static string GetEmail(string fallback = "")
    {
        var userData = playport.Instance.GetCurrentUserData();
        return userData.email ?? fallback;
    }

    /// <summary>
    /// Get user ID, returns empty string if not available
    /// </summary>
    public static string GetUserId(string fallback = "")
    {
        if (playport.Instance != null)
        {
            var userData = playport.Instance.GetCurrentUserData();
            if (userData != null)
            {
                return userData.id ?? fallback;
            }
        }
        return fallback;
    }

    /// <summary>
    /// Check if user data is available
    /// </summary>
    public static bool IsDataAvailable()
    {
        return playport.Instance != null && 
               playport.Instance.GetCurrentUserData() != null;
    }

    /// <summary>
    /// Get full user data object (null-safe)
    /// </summary>
    public static playport.UserData GetUserData()
    {
        if (playport.Instance != null)
        {
            return playport.Instance.GetCurrentUserData();
        }
        return null;
    }

    
    public static bool IsEnoughCoins(int requiredCoins)
    {
        int currentCoins = GetCoins(0);
        return currentCoins >= requiredCoins;
    }

    public static void BetTransactions(int betAmount, string result)
    {
        // "debit" | "credit"
        if (playport.Instance != null)
        {
            playport.Instance.StartCoroutine(
                playport.Instance.PostBetTransactionCoroutine(betAmount, result)
            );
        }
    }
  
    /// <summary>
    /// Record a win (null-safe)
    /// </summary>
    public static void RecordWin()
    {
        if (playport.Instance != null)
        {
            playport.Instance.UpdateWins();
        }
        else
        {
            Debug.LogWarning("playport not available, cannot record win");
        }
    }

    /// <summary>
    /// Record a loss (null-safe)
    /// </summary>
    public static void RecordLoss()
    {
        if (playport.Instance != null)
        {
            playport.Instance.UpdateLosses();
        }
        else
        {
            Debug.LogWarning("playport not available, cannot record loss");
        }
    }

    public static void ProfileUpdate()
    {
        if (playport.Instance != null)
        {
            playport.Instance.GetUserProfile();
        }
    }
}
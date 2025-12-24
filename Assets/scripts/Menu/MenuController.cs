using CardGame;
using Photon.Pun;
using Photon.Realtime;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Data.Common;
using System.Runtime.InteropServices;


namespace CardGame 
{
    
public class MenuController : MonoBehaviour
{
    public static MenuController Instance;

    [SerializeField] private RectTransform Header;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private Slider loadingBar;

    [SerializeField] private RectTransform vsPanel;

    private bool isConnecting = false;
    private float connectionTimeout = 15f;


    public int OpponentS;
    public Animator dp;
    public Animator coinflow;
    public Animator coinflow1;
    public Animator numlaod;
    public Animator nameload;
    public Animator coinBonus;

    public AudioClip coinCollectingSound;
    public AudioClip profileSearchSound;
    public AudioSource sfxSource;


    [SerializeField] private TextMeshProUGUI vsTotalBetText;
    [SerializeField] private TextMeshProUGUI vsAwayUsernameText;
    [SerializeField] private Image OpponentprofileImage;    
    [SerializeField] private Sprite opponentprofilesprite;


        void Awake()
            {
                if (Instance == null)
                {
                    Instance = this;
                }
            }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            loadingPanel.gameObject.SetActive(true);
            ShowLoadingScreenWithProgress();
        }

        public void RoomsBtn(string room)
        {
            // Load the main game scene
            PhotonController.Instance.whichRoom = room;
            PhotonController.Instance.FindRoom();
            vsPanel.gameObject.SetActive(true);
            OpponentStatus = 0;
            SetUserData();
            
        }

        public void OnPlayerJoinnedRoom()
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

                
            }

            SetOS();

            StartCoroutine(startGameCoroutine());

        }    

        IEnumerator startGameCoroutine()
        {
            yield return new WaitForSeconds(4f);
            SceneManager.LoadScene("Game");
        }

        public void VsJoinedRoom()
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
            {
                StartCoroutine(SetOpponent());
            }
        }

        IEnumerator SetOpponent()
        {
            yield return new WaitForSeconds(1f);
            SetOS();
        }


        public void ShowLoadingScreenWithProgress()
        {
            loadingPanel.gameObject.SetActive(true);
            if (loadingBar != null)
            {
                loadingBar.value = 0;
            }
            isConnecting = true;
            StartCoroutine(NetworkLoadingProcess());
        }

        private IEnumerator NetworkLoadingProcess()
    {
        bool networkConnected = false;
        bool profileFetched = false;
        float loadingProgress = 0f;
        float timeElapsed = 0f;

        // Start fetching user profile
        if (playport.Instance != null)
        {
            StartCoroutine(WaitForProfileFetch(() => profileFetched = true));
        }
        else
        {
            Debug.LogWarning("playport Instance not found. Skipping profile fetch.");
            profileFetched = true; // Skip if not available
        }

        // Update loading bar while waiting for both connections
        while ((!networkConnected || !profileFetched) && timeElapsed < connectionTimeout)
        {
            // Check if network is connected
            if (PhotonNetwork.InLobby)
            {
                networkConnected = true;
            }

            timeElapsed += Time.deltaTime;

            // Calculate progress based on both processes
            float networkProgress = networkConnected ? 0.5f : Mathf.Clamp01(timeElapsed / connectionTimeout) * 0.4f;
            float apiProgress = profileFetched ? 0.5f : Mathf.Clamp01(timeElapsed / connectionTimeout) * 0.4f;
            float timeProgress = networkProgress + apiProgress; // Max 85% combined

            float targetProgress = (networkConnected && profileFetched) ? 1f : timeProgress;

            // Smooth progress bar animation
            loadingProgress = Mathf.Lerp(loadingProgress, targetProgress, Time.deltaTime * 2f);

            if (loadingBar != null)
            {
                loadingBar.value = loadingProgress;
            }

            yield return null;
        }

        // Check if both connections were successful
        if (networkConnected && profileFetched)
        {
            // Final animation to complete
            while (loadingBar != null && loadingBar.value < 0.99f)
            {
                loadingBar.value = Mathf.Lerp(loadingBar.value, 1f, Time.deltaTime * 5f);
                yield return null;
            }

            // Small delay before hiding loading screen
            yield return new WaitForSeconds(0.3f);
            loadingPanel.gameObject.SetActive(false);
            Header.gameObject.SetActive(true);
        }
        else
        {
                // Connection timeout - the existing retry logic will handle reconnection
                // Keep loading panel visible
                Debug.LogWarning($"Loading timeout - Network: {networkConnected}, Profile: {profileFetched}");
            
                //MessagePanel.gameObject.SetActive(true);
                //MessageText.text = "Loading timeout... ";
                //MessageCloseButton.onClick.RemoveAllListeners();
                //MessageCloseButton.onClick.AddListener(() => { MessagePanel.gameObject.SetActive(false); });
                
        }

        isConnecting = false;
    }

    // Helper coroutine to wait for profile fetch
    private IEnumerator WaitForProfileFetch(System.Action onComplete)
    {
        // Wait a frame to ensure playport is initialized
        yield return null;

        if (playport.Instance == null)
        {
            Debug.LogWarning("playport Instance is null");
            onComplete?.Invoke();
            yield break;
        }

        // Store the initial user data state
        var initialUserData = playport.Instance.MyUserData;

        // Start the profile fetch if not already started
        if (playport.Instance.MyUserData == null)
        {
            playport.Instance.GetUserProfile();
        }

        // Wait until MyUserData is populated
        float timeout = connectionTimeout;
        float elapsed = 0f;

        while (playport.Instance.MyUserData == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Check if profile was successfully fetched
        if (playport.Instance.MyUserData != null)
        {
            Debug.Log("Profile fetched successfully during loading");
        }
        else
        {
            Debug.LogWarning("Profile fetch timed out during loading");
        }

        onComplete?.Invoke();
    }


    public void SetUserData()
    {
        PhotonNetwork.NickName = PlayportDataHelper.GetUsername();
        string purl = PlayportDataHelper.GetProfileUrl();
        ExitGames.Client.Photon.Hashtable playerData = new ExitGames.Client.Photon.Hashtable();
        playerData["profile_image_url"] = purl;   // store your own URL
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerData);
    }


    private void UpdateVsNames()
    {

        // Away (opponent)
        var others = PhotonNetwork.PlayerListOthers;
        if (others != null && others.Length > 0)
        {
            string awayName = others[0].NickName;
            if (vsAwayUsernameText) vsAwayUsernameText.text = awayName;
            else vsAwayUsernameText.text = "";

            // ✅ Load opponent image
            if (others[0].CustomProperties.ContainsKey("profile_image_url"))
            {
                string imageUrl = others[0].CustomProperties["profile_image_url"].ToString();
                Debug.Log($"[MainMenu] Opponent profile image URL: {imageUrl}");
                PlayerPrefs.SetString("opponentimage", imageUrl);
                StartCoroutine(LoadOpponentImage(imageUrl));
            }
            else
            {
                Debug.LogWarning("[MainMenu] Opponent has no profile image property set.");
            }
        }
    }

        private void Handle401Unauthorized()
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

    public IEnumerator LoadOpponentImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogWarning("[OpponentImage] URL is empty.");
            OpponentprofileImage.sprite = opponentprofilesprite;
            //PlayerImg.sprite = sampleProfile;
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();
            if (CheckForUnauthorized(request))
            {
                yield break;
            }
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[OpponentImage] Error loading opponent image: {request.error}");
                OpponentprofileImage.sprite = opponentprofilesprite;
                //PlayerImg.sprite = sampleProfile;
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            Sprite sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));

            if (OpponentprofileImage)
            {
                OpponentprofileImage.sprite = sprite;
                Debug.Log("[OpponentImage] Opponent image loaded successfully.");
            }
        }
    }

    private int _opponentStatus;

    public int OpponentStatus
    {
        get { return _opponentStatus; }
        set
        {
            
          string  RoomName = PhotonController.Instance.whichRoom;
          int mainPlayerPrize = 0;

            if(RoomName == "galle")
            {
                mainPlayerPrize = 100;
            }
            else if(RoomName == "kandy")
            {
                mainPlayerPrize = 200;
            }
            else if(RoomName == "colombo")
            {
                mainPlayerPrize = 500;
            }
            else if(RoomName == "jaffna")
            {
                mainPlayerPrize = 1000;
            }
            else if(RoomName == "sigiri")
            {
                mainPlayerPrize = 5000;
            }


            
            _opponentStatus = value;

            if (_opponentStatus == 0)
            {
                dp.Play("0");
                coinflow.Play("0");
                coinflow1.Play("0");
                numlaod.Play("0");
                nameload.Play("0");
                vsAwayUsernameText.gameObject.SetActive(false);
                coinBonus.Play("0");
                vsTotalBetText.text = mainPlayerPrize.ToString("###,###,###");
                PlayRollingSound();
                OpponentprofileImage.gameObject.SetActive(false);
            }
            else if (_opponentStatus == 1)
            {
                OpponentprofileImage.sprite = opponentprofilesprite;
                StopRollingSound();
                PlayCoinCollectSound();
                dp.Play("1");
                coinflow.Play("1");
                coinflow1.Play("1");
                numlaod.Play("1");
                nameload.Play("1");
                vsAwayUsernameText.gameObject.SetActive(true);
                //vsAwayUsernameText.text = NetworkManager.opponentPlayer.userName;
                coinBonus.Play("1");
                OpponentprofileImage.gameObject.SetActive(true);
                
                
                LeanTween.value(mainPlayerPrize, mainPlayerPrize * 2, 1f).setOnUpdate((float val) =>
                {
                    vsTotalBetText.text = "" + (int)val;
                }).setOnComplete(() =>
                {
                    

                });


            }
            else if (_opponentStatus == 2)
            {
                dp.Play("1");
                coinflow.Play("0");
                coinflow1.Play("0");
                numlaod.Play("0");
                nameload.Play("1");
                coinBonus.Play("1");
                //vsAwayUsernameText.text = NetworkManager.opponentPlayer.userName;
                
            }
            else if (_opponentStatus == 3)
            {
                dp.Play("1");
                coinflow.Play("0");
                coinflow1.Play("0");
                numlaod.Play("0");
                nameload.Play("1");
                coinBonus.Play("1");
            }
        }
    }

    public void endNumberload()
    {
        OpponentStatus = 2;
    }

    public void SetOS()
    {
        OpponentStatus = 1;
        UpdateVsNames();
    }

    public void PlayCoinCollectSound()
    {
        PlaySound(coinCollectingSound);
    }
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayRollingSound()
        {
            sfxSource.clip = profileSearchSound;
            sfxSource.loop = true;
            sfxSource.Play();
        }

    public void StopRollingSound()
        {
            sfxSource.Stop();
            sfxSource.loop = false;
            sfxSource.clip = null;
        }



}
}
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

    public GameObject StartButton;

    public static MenuController Instance;


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
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RoomsBtn(string room)
    {
        // Load the main game scene
        PhotonController.Instance.whichRoom = room;
        PhotonController.Instance.FindRoom();
        
    }

    public void OnPlayerJoinnedRoom()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }

        SceneManager.LoadScene("Game");

    }
}
}
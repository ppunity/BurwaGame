using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using UnityEditor;

namespace CardGame{
    public class PhotonController : MonoBehaviourPunCallbacks {
        public static PhotonController Instance;
        public string whichRoom;
        public int roomEntryPice = 0;


        void Awake() {
            if (Instance == null) {
                Instance = this;
            }
        }

        public void Start() {
            PhotonNetwork.KeepAliveInBackground = 60;
            if (!PhotonNetwork.IsConnectedAndReady) {
                PhotonNetwork.ConnectUsingSettings();
            } else {
                JoinLobby();
            }
        }

        public override void OnConnectedToMaster() {
            if (PlayerPrefs.HasKey("username")){
                PhotonNetwork.NickName = PlayerPrefs.GetString("username");
            }
            
            JoinLobby();
        }

        public void JoinLobby() {
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby(){
            Debug.Log("joined lobby");

            MenuController.Instance.StartButton.SetActive(true);
        }

        public override void OnLeftLobby(){
            
        }

        public void FindRoom() {
            //MenuController.Instance.vsMsgText.text = "Searching room...";

            ExitGames.Client.Photon.Hashtable roomHastable = new ExitGames.Client.Photon.Hashtable ();
            roomHastable.Add("roomType", whichRoom);
            roomHastable.Add("gameName", "BurwaCardGame");
            

            PhotonNetwork.JoinRandomRoom(roomHastable, 2);
        }

        public void CreatePracticeRoom(){
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "roomType" };
            roomOptions.IsOpen = false;
            roomOptions.IsVisible = false;
            PhotonNetwork.CreateRoom(null, roomOptions);
        }

        public override void OnJoinRandomFailed(short returnCode, string message) {
            ExitGames.Client.Photon.Hashtable roomHastable = new ExitGames.Client.Photon.Hashtable ();
            roomHastable.Add("roomType", whichRoom);
            roomHastable.Add("gameName", "BurwaCardGame");

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.CustomRoomProperties = roomHastable;
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "roomType" , "gameName" };
            roomOptions.IsOpen = true;
            roomOptions.IsVisible = true;
            PhotonNetwork.CreateRoom(null, roomOptions);
        }

        public override void OnJoinedRoom()
        {
           ExitGames.Client.Photon.Hashtable userHastable = new ExitGames.Client.Photon.Hashtable();
                if (PhotonNetwork.PlayerList.Length == 1)
                {
                    userHastable.Add("tag", "NoneDealer");
                }
                else
                {
                    userHastable.Add("tag", "Dealer");
                }

                PhotonNetwork.LocalPlayer.SetCustomProperties(userHastable);
                //MenuController.Instance.VsJoinedRoom();
                PhotonNetwork.AutomaticallySyncScene = true;
            
            Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name + " | Players: " + PhotonNetwork.CurrentRoom.PlayerCount);
        }

        public override void OnLeftRoom() {
            PhotonNetwork.AutomaticallySyncScene = false;

            if (SceneManager.GetActiveScene().name == "Menu") {
                //MenuController.Instance.VsOnLeftRoom();
            } else if (SceneManager.GetActiveScene().name == "Game" || SceneManager.GetActiveScene().name == "Practice"){
                SceneManager.LoadScene("Menu");
            }
        }

        public override void OnCreatedRoom() {
            
        }

        public override void OnPlayerEnteredRoom(Player newPlayer) {
            if (PhotonNetwork.IsMasterClient) {
                MenuController.Instance.OnPlayerJoinnedRoom();
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer) {
            if (SceneManager.GetActiveScene().name == "Game") {
                CardManager.Instance.CheckRoomPlayers();
            }
        }

        public override void OnDisconnected(DisconnectCause cause) {
            PhotonNetwork.LeaveRoom();
            
        }

    }
}
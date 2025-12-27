
using CardGame;
using UnityEngine;
using TMPro; 
public class wintext : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI WinText;

    [SerializeField] GameObject ShareBtn;

    public void WinTextUpdate()
    {

        string RoomName = PhotonController.Instance.whichRoom;
        int roomPrize =100;

        if(RoomName == "galle")
        {
            roomPrize = 200;
        }
        else if(RoomName == "kandy")
        {
            roomPrize = 400;
        }
        else if(RoomName == "colombo")
        {
            roomPrize = 1000;
        }
        else if(RoomName == "jaffna")
        {
            roomPrize = 2000;
        }
        else if(RoomName == "sigiri")
        {
            roomPrize = 10000;
        }
        LeanTween.value(0, roomPrize * 0.9f, 1f).setOnUpdate(
                                (float val) =>
                                {
                                    if (val < 1)
                                    {
                                        WinText.text = "0";
                                    }
                                    else
                                    {
                                        WinText.text = val.ToString("N2") + " LKR";
                                    }
                                });
    }
    
    public void ShowShareButton()
    {
        ShareBtn.SetActive(true);
    }
}

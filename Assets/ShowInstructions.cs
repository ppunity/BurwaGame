using UnityEngine;

public class ShowInstructions : MonoBehaviour
{
    [SerializeField] private GameObject Game;
    [SerializeField] private GameObject Instructions;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ShowGame()
    {
        //Instructions.SetActive(false);
        //Game.SetActive(true);

        Game.GetComponent<RectTransform>().localScale = new Vector3(0.3f,1,1);

        
        LeanTween.scale(Instructions.GetComponent<RectTransform>(), new Vector3(0,1,1), 0.1f)
        .setEase(LeanTweenType.linear)
        .setOnComplete(() =>
        {
            // Optional: actions after scaling completes
            Instructions.SetActive(false);
            Game.SetActive(true);

                    LeanTween.scale(Game.GetComponent<RectTransform>(), new Vector3(1,1,1), 0.1f)
                    .setEase(LeanTweenType.linear)
                    .setOnComplete(() =>
                    {
                        
                        
                    });


        });
    }
    
    public void ShowInstructionsMenu()
    {
        //Game.SetActive(false);
        //Instructions.SetActive(true);
        Instructions.GetComponent<RectTransform>().localScale = new Vector3(0.3f,1,1);

        
        LeanTween.scale(Game.GetComponent<RectTransform>(), new Vector3(0,1,1), 0.1f)
        .setEase(LeanTweenType.linear)
        .setOnComplete(() =>
        {
            // Optional: actions after scaling completes
            Game.SetActive(false);
            Instructions.SetActive(true);

                    LeanTween.scale(Instructions.GetComponent<RectTransform>(), new Vector3(1,1,1), 0.1f)
                    .setEase(LeanTweenType.linear)
                    .setOnComplete(() =>
                    {
                        
                        
                    });


        });
    }
}

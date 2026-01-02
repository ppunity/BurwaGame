using UnityEngine;

public class shadow : MonoBehaviour
{
    [SerializeField] GameObject target;

    // Update is called once per frame
    void Update()
    {   
        gameObject.SetActive(target.transform.childCount >0);
    }
}

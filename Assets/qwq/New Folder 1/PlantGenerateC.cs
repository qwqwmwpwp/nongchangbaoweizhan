using UnityEngine;

public class PlantGenerateC : MonoBehaviour
{
    public static PlantGenerateC instance;
    GameObject bass;
    public GameObject plant1;
    private void Awake()
    {
        if (instance)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }
   
    public void enterUI(GameObject bass)
    {
        this.bass = bass;
        PlantGenerateM.instance.enterUI(bass.transform.position);

    }
    public void PlantGenerated()
    {
        if (!bass)
            return;
        Instantiate(plant1,bass.transform);
    }
}

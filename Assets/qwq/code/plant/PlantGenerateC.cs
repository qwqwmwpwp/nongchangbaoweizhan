using UnityEngine;

public class PlantGenerateC : MonoBehaviour
{
    public static PlantGenerateC instance;
    BatteryBase bass;
    public GameObject plant1;
    [SerializeField] PlantGenerateM plantGenerateM;
    private void Awake()
    {
        if (instance) Destroy(this);
        else instance = this;
    }

    public void enterUI(BatteryBase bass)
    {
        this.bass = bass;
        plantGenerateM.enterUI(bass.transform.position);

    }
    public void PlantGenerated()
    {
        if (!bass && !bass.IsGenerated()) return;
        Instantiate(plant1, bass.parentObject.transform);
    }
}

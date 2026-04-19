using qwq;
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
        BatteryCtx ctx;
        if (plant1.GetComponent<Battery>().ctx is BatteryCtx newCtx) ctx = newCtx;
        else return;

        bool isfertilizer = AttributeManager.Instance.SpendMoney(ctx.fertilizer,ctx.diamond);

        if (isfertilizer && bass &&bass.IsGenerated()) 
        Instantiate(plant1, bass.parentObject.transform);

    }
}

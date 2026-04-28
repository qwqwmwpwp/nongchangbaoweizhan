using UnityEngine;

public class SoldierDetection : MonoBehaviour
{
    [SerializeField] BambooSoldier bambooSoldier;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<IDamageable>() is IDamageable enemy)
            bambooSoldier.ctx.enemys.Add(enemy);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<IDamageable>() is IDamageable enemy)
            bambooSoldier.ctx.enemys.Remove(enemy);
    }
}

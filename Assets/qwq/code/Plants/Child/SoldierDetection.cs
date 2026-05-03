using UnityEngine;
using qwq;

public class SoldierDetection : MonoBehaviour
{
    [SerializeField] BambooSoldier bambooSoldier;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponentInParent<Enemy>();
        if (enemy == null)
            return;
        bambooSoldier.ctx.enemys.Add(enemy);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponentInParent<Enemy>();
        if (enemy == null)
            return;
        bambooSoldier.ctx.enemys.Remove(enemy);
    }
}

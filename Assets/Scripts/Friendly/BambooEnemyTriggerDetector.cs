using qwq;
using UnityEngine;

[DisallowMultipleComponent]
public class BambooEnemyTriggerDetector : MonoBehaviour
{
    [SerializeField] private Bamboo ownerBamboo;

    public void SetOwner(Bamboo bamboo)
    {
        ownerBamboo = bamboo;
    }

    private void Awake()
    {
        if (ownerBamboo == null)
            ownerBamboo = GetComponentInParent<Bamboo>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        BambooCtx ctx = ownerBamboo?.ctx;
        if (ctx == null)
            return;

        Enemy enemy = collision.GetComponentInParent<Enemy>();
        if (enemy == null)
            return;

        ctx.RegisterEnemyInRange(enemy);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        BambooCtx ctx = ownerBamboo?.ctx;
        if (ctx == null)
            return;

        Enemy enemy = collision.GetComponentInParent<Enemy>();
        if (enemy == null)
            return;

        ctx.UnregisterEnemyInRange(enemy);
    }
}

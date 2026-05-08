using qwq;
using System.Collections.Generic;
using UnityEngine;

public class WatermelonBullet : MonoBehaviour
{
    [SerializeField] GameObject bulletSprite;
    [SerializeField] GameObject rangeSprite;

    List<IDamageable> enemys = new();
    CircleCollider2D circleCollider2D;
    [SerializeField] int attack = 2;
    [SerializeField] float speed = 5; // 初速度大小
    Vector2 v;
    [SerializeField] float detection_max;
    float detection_t;//
    Rigidbody2D rb;
    Vector2 target;
    int currentMethod = -1;



    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        circleCollider2D = GetComponent<CircleCollider2D>();

    }

    private void Update()
    {
        switch (currentMethod)
        {
            case 0:
                detection_t = detection_max;
                currentMethod++;
                break;
            case 1:
                Move(Time.deltaTime);
                break;
            case 2:
                Detection(Time.deltaTime);
                break;
            case 3:
                Attack(Time.deltaTime);
                break;
            case 4:
                currentMethod = -1;
                Destroy(gameObject);
                break;
        }

      
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponentInParent<Enemy>();
        if (enemy == null || !enemy.IsInteractable)
            return;
        enemys.Add(enemy);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //Enemy enemy = collision.GetComponentInParent<Enemy>();
        //if (enemy == null)
        //    return;
        //enemys.Remove(enemy);
    }

    public void Initialize(int attack, float Range, Vector2 target,bool isaa=false)
    {
        this.attack = attack;
        circleCollider2D.radius = Range/2;
        rangeSprite.transform.localScale = Vector2.one * Range;

        this.target = target;
        v = speed * Vector2.up;
        if (currentMethod == -1)
            currentMethod = 0;
    }

    private void Move(float deltaTime)
    {
        // 使用局部变量减少属性访问
        Vector2 currentPosition = transform.position;
        Vector2 toTarget = target - currentPosition;

        // 使用平方距离避免开方运算
        float sqrDistance = toTarget.sqrMagnitude;

        if (sqrDistance < 0.1f) // 0.1² = 0.01
        {
            StopAndComplete();
            return;
        }

        // 计算一次距离，避免重复计算
        float distance = Mathf.Sqrt(sqrDistance);
        Vector2 targetDirection = toTarget / distance;

        // 使用更高效的转向逻辑
        Vector2 desiredVelocity = targetDirection * speed;

        // 动态加速度：距离越远加速越快
        float accelerationFactor = 4f;
        if (distance < 1f)
        {
            accelerationFactor = Mathf.Lerp(2f, 4f, distance);
        }

        Vector2 steering = (desiredVelocity - v) * accelerationFactor * deltaTime;
        v += steering;

        // 更高效的速度限制
        float sqrSpeed = v.sqrMagnitude;
        float maxSqrSpeed = speed * speed;
        if (sqrSpeed > maxSqrSpeed)
        {
            v *= speed / Mathf.Sqrt(sqrSpeed);
        }

        rb.velocity = v;
    }

    private void StopAndComplete()
    {
        rb.velocity = Vector2.zero;
        circleCollider2D.enabled = true;
        bulletSprite.SetActive(false);
        rangeSprite.SetActive(true);
        currentMethod++;
    }
    private void Detection(float deltaTime)
    {

        if (detection_t <= 0)
        {
            currentMethod++;

        }
        detection_t -= deltaTime;
    }

    private void Attack(float deltaTime)
    {
        AttackMusic();
        enemys.RemoveAll(e => !DamageableTargetUtility.IsValid(e));

        List<IDamageable> enemiesToAttack = new List<IDamageable>(enemys);
        foreach (var e in enemiesToAttack)
        {
            if (DamageableTargetUtility.IsValid(e))
            {
                Debug.Log(e);
                e.TakeDamage(attack);
            }
        }
        circleCollider2D.enabled = false;

        currentMethod++;
    }


    [Header("音效")]
    [SerializeField] AudioClip attackClip;
    public void AttackMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISound(attackClip);
        }
    }
}

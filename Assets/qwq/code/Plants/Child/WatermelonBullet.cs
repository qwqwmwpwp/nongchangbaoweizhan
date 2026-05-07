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
        // 计算到目标的向量
        Vector2 toTarget = target - (Vector2)transform.position;
        float distance = toTarget.magnitude;

        // 距离足够近时停止
        if (distance < 0.1f)
        {
            rb.velocity = Vector2.zero;
            circleCollider2D.enabled = true;
            bulletSprite.SetActive(false);
            rangeSprite.SetActive(true);
            currentMethod++;
            return;
        }

        // 简化移动逻辑
        Vector2 targetDirection = toTarget / distance; // 避免重复normalized
        float maxSpeed = speed;

        // 使用更简单的加速/减速逻辑
        Vector2 desiredVelocity = targetDirection * maxSpeed;
        Vector2 steering = (desiredVelocity - v) * 6f * deltaTime;

        v = Vector2.ClampMagnitude(v + steering, maxSpeed);
        rb.velocity = v;
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
        enemys.RemoveAll(e => e == null ||e.Equals(null) ||e.obj == null);

        List<IDamageable> enemiesToAttack = new List<IDamageable>(enemys);
        foreach (var e in enemiesToAttack)
        {
            if (e != null && e.obj != null)  
            {
                Debug.Log(e);
                e.TakeDamage(attack);
            }
        }
        circleCollider2D.enabled = false;

        currentMethod++;
    }

}

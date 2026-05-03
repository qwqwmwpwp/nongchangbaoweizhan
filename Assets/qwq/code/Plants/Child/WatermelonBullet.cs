using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WatermelonBullet : MonoBehaviour
{
    [SerializeField] GameObject bulletSprite;
    [SerializeField] GameObject rangeSprite;

    List<IDamageable> enemys = new();
    CircleCollider2D circleCollider2D;
    [SerializeField] int attack = 2;
    [SerializeField] float speed = 5;
    [SerializeField] float t_max;
    float t;
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
                t = t_max;
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
        if (collision.GetComponent<IDamageable>() is IDamageable enemy)
        {
            enemys.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<IDamageable>() is IDamageable enemy)
        {
            enemys.Remove(enemy);
        }
    }

    public void Initialize(int attack, float Range, IDamageable target)
    {
        this.attack = attack;
        circleCollider2D.radius = Range/2;
        rangeSprite.transform.localScale = Vector2.one * Range;

        this.target = target.obj.transform.position;
        if (currentMethod == -1)
            currentMethod = 0;
    }

    private void Move(float deltaTime)
    {
        float magnitude = (target - (Vector2)transform.position).magnitude;
        if (magnitude < 0.1)
        {
            rb.velocity = Vector2.zero;
            circleCollider2D.enabled = true;
            bulletSprite.SetActive(false);
            rangeSprite.SetActive(true);

            currentMethod++;
            return;
        }
        rb.velocity = (target - (Vector2)transform.position).normalized * speed;
    }

    private void Detection(float deltaTime)
    {

        if (t <= 0)
        {
            currentMethod++;

        }
        t -= deltaTime;
    }

    private void Attack(float deltaTime)
    {
        enemys.RemoveAll(e => e == null || e.obj == null);

        // 在遍历前创建列表的深拷贝
        List<IDamageable> enemiesToAttack = new List<IDamageable>(enemys);
        foreach (var e in enemiesToAttack)
        {
            if (e != null && e.obj != null)  // 添加安全检查
            {
                Debug.Log(e);
                e.TakeDamage(attack);
            }
        }
        circleCollider2D.enabled = false;

        currentMethod++;
    }

}

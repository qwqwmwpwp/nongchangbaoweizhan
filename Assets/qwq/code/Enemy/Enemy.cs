using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace qwq
{
    public class Enemy : MonoBehaviour, IDamageable
    {
        public GameObject Object => gameObject;
        [Header(" Ù–‘")]
        public int attack = 1;
        public int speed = 2;
        [SerializeField] int hp_max = 100;
        int hp = 10;
        Rigidbody2D rb;
        public Vector3 targetPosition;
        private int wayPointIndex = 0;
        [Header("UI")]
        public EnemyHealthUI enemyHealthUI;
        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            hp = hp_max;
            enemyHealthUI.PlayerHealthChange(hp, hp_max);
            targetPosition = WayPointManager.Instance.points[wayPointIndex].transform.position;
        }
        public void Update()
        {
            Vector3 dic = (targetPosition - gameObject.transform.position).normalized;
            rb.velocity = dic * speed;
            if (Vector3.Distance(transform.position, targetPosition) <= 0.5 && wayPointIndex != WayPointManager.Instance.points.Count - 1)
            {
                wayPointIndex++;
                targetPosition = WayPointManager.Instance.points[wayPointIndex].transform.position;
            }
        }
        public void TakeDamage(int amount)
        {
            hp -= amount;
            enemyHealthUI.PlayerHealthChange(hp, hp_max);
            if (hp <= 0) Death();
        }

        public int Attack()
        {
            Death();
            return attack;
        }

        public void Death()
        {
            Destroy(gameObject);
        }

    }
}

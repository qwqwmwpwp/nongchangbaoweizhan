using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace qwq{
    public class Enemy : MonoBehaviour, IDamageable
    {
        [SerializeField] float HP = 100;
        public int attack = 1;
        Rigidbody2D rb;
        public GameObject Object => gameObject;

        private void Start()
        {
            rb=GetComponent<Rigidbody2D>();
        }
        public void Update()
        {
            rb.velocity = new(1, 0);
        }
        public void TakeDamage(int amount)
        {
            HP -= amount;
        }
        public int Attack()
        {
            Destroy(gameObject);
            return attack;
        }
    }
}
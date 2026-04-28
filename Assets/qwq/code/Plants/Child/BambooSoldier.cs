using HSM;
using qwq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BambooSoldier : MonoBehaviour
{
    public BambooSoldierCtx ctx;
    float attackInterval;
    
    protected StateMachine machine;
    protected State root;

    private void Awake()
    {
        ctx.rb = GetComponent<Rigidbody2D>();
        ctx.anim = GetComponent<Animator>();
        ctx.transform = transform;

        root = new BambooSoldierRoot(null,ctx);
        StateMachineBuilder builder = new(root);
        machine = builder.Build();
    }
    private void Update()
    {
        if (ctx.enemys.Count < 1) ctx.enemy = null;
        else ctx.enemy = ctx.enemys
              .Where(e => e != null)
              .OrderBy(e => Vector2.Distance(transform.position, e.obj.transform.position))
              .FirstOrDefault();

        machine.Tick(Time.deltaTime);

        //if (attackInterval > 0)
        //    attackInterval -= Time.deltaTime;

        //ctx.enemys.RemoveAll(enemy => enemy == null);
        //ctx.rb.velocity = Vector2.zero;


        //  // ÕÒ×î½üµÐÈË
        //  var nearestEnemy = ctx.enemys
        //      .Where(e => e != null)
        //      .OrderBy(e => Vector2.Distance(transform.position, e.obj.transform.position))
        //      .FirstOrDefault();


        //  if (nearestEnemy == null) return;

        //  Vector2 toEnemy = nearestEnemy.obj.transform.position - transform.position;
        //  float distance = toEnemy.magnitude;

        //  if (distance > ctx.attack_r)
        //  {
        //      ctx.rb.velocity = toEnemy.normalized * ctx.speed;
        //      float x =transform.localScale.x;
        //      if (toEnemy.x > 0) x = 1;
        //      else x = -1;

        //      transform.localScale=new(x,1, 1);
        //  }
        //  else if (attackInterval <= 0)
        //  {
        //      nearestEnemy.TakeDamage(ctx.attack);
        //      attackInterval = ctx.AttackInterval;
        //      ctx.anim.SetTrigger("attack");
        //  }
    }

}

[Serializable]
public class BambooSoldierCtx
{
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Animator anim;
    [HideInInspector] public Transform transform;
    public float AttackInterval = 0.5f;
    public float speed = 3f;
    public int attack = 2;
    public float attack_r;
    [Header("1")]
    public int attack1 = 2;
    [Header("2")]
    public int attack2 = 3;
    [Header("3")]
    public int attack3 = 4;

    public List<IDamageable> enemys = new();
    public  IDamageable enemy;
}

namespace HSM
{
    public class BambooSoldierRoot : State
    {
        public BambooSoldierMove Move;
        public BambooSoldierAttack Attack;
        public BambooSoldierRoot(StateMachine machine, BambooSoldierCtx ctx) : base(machine, null)
        {
            Move = new BambooSoldierMove(machine, this, ctx);
            Attack = new BambooSoldierAttack(machine, this, ctx);

        }

        protected override State GetInitialState()
        {
            return Move;
        }
    }

    public class BambooSoldierMove : State
    {
        BambooSoldierCtx Ctx;
        public BambooSoldierMove(StateMachine machine, State parent ,BambooSoldierCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }
        protected override State GetTransition()
        {
            if (Ctx.enemy == null)
            {
                return null;
            }
            Vector2 toEnemy = Ctx.enemy.obj.transform.position -Ctx.transform.position;
            float distance = toEnemy.magnitude;

            if (distance > Ctx.attack_r)
            {
                return ((BambooSoldierRoot)Parent).Attack;
            }


            return null;
        }
    }

    public class BambooSoldierAttack : State
    {
        BambooSoldierCtx Ctx;
        public BambooSoldierAttack(StateMachine machine, State parent, BambooSoldierCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }
    }
}


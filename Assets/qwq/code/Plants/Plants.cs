using HSM;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace qwq
{
    public abstract class Plants : MonoBehaviour, IBatteryBackward
    {
        protected StateMachine machine;
        protected State root;

        public abstract PlantsCtx plantsCtx { get; }

        protected virtual void Awake()
        {
            StateMachineBuilder builder = new(root);
            machine = builder.Build();
        }
        protected virtual void OnEnable()
        {
            GameEvent.EnemyRewindRequested += HandleRewindRequested;
        }

        protected virtual void OnDisable()
        {
            GameEvent.EnemyRewindRequested -= HandleRewindRequested;
        }

        protected virtual void Update()
        {
            if (plantsCtx.partialBacktracking_t > 0)
                plantsCtx.partialBacktracking_t -= Time.deltaTime;
            else if (plantsCtx.backward >= 4)
                plantsCtx.Death();

            if (plantsCtx.globalBacktracking_t > 0)
            {
                plantsCtx.globalBacktracking_t -= Time.deltaTime;
                if (plantsCtx.partialBacktracking_t > 0)
                    plantsCtx.partialBacktracking_t = 0.01f;
            }

            machine.Tick(Time.deltaTime);
        }

        private void HandleRewindRequested(float arg1, float arg2)
        {
            if (plantsCtx.globalBacktracking_t > 0)
                return;

            plantsCtx.globalBacktracking_t = arg1;
        }


        public virtual void Backward(float t) {
            plantsCtx.partialBacktracking_t = t;
            plantsCtx.backward++;
        }

    }

    public class PlantsCtx : IWeapon
    {
        public Sprite UI;
        [HideInInspector] public GameObject plant;

        [HideInInspector] public Transform transform;
        public List<IDamageable> enemys = new();
        public virtual void Fire(IDamageable target) { }
        [field: SerializeField] public int fertilizer { get; private set; }//
        [field: SerializeField] public int diamond { get; private set; }//

        [Header("全局回溯")]
        public float globalBacktracking_t = 0;
        [Header("局部回溯")]
        public float partialBacktracking_t = 0;
        [HideInInspector] public int backward = 0;//回溯次数
        [Header("生长加速")]
        public int catalysisNumber = 0;//次数

        public void Death()
        {
            GameObject.Destroy(plant);
        }
    }



    public interface IBatteryBackward
    {
        public void Backward(float t);
    }
}

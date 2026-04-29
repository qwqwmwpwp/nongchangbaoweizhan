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
            machine.Tick(Time.deltaTime);
        }

        private void HandleRewindRequested(float arg1, float arg2)
        {
            Backward(3f);
        }

        public virtual void Backward(float t) { }

    }

    public class PlantsCtx : IWeapon
    {
        public Sprite UI;
        [HideInInspector] public Transform transform;
        public List<IDamageable> enemys = new();
        public virtual void Fire(IDamageable target) { }
        [field: SerializeField] public int fertilizer { get; private set; }//
        [field: SerializeField] public int diamond { get; private set; }//

    }
    public interface IBatteryBackward
    {
        public void Backward(float t);
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine.Animations;
using UnityEngine;
using System;

namespace HSM
{
    public abstract class State //状态树节点
    {
        public readonly StateMachine Machine;//发送状态转换请求
        public readonly State Parent;//回溯到父级状态节点
        public State ActiveChild;//活跃子节点
        protected bool isActiveChildExit = true;
        readonly List<IActivity> activities = new List<IActivity>();
        public IReadOnlyList<IActivity> Activities => activities;

        public State(StateMachine machine, State parent = null)
        {
            Machine = machine;
            Parent = parent;
        }

        public void Add(IActivity a) { if (a != null) activities.Add(a); }

        protected virtual State GetInitialState() => null;//默认激活的子状态(nul=l则为最终状态节点)

        protected virtual State GetTransition() => null;//当前帧是否需要切换状态 (null=不需要切换)

        // 生命周期钩子
        protected virtual void OnEnter() { }

        protected virtual void OnExit() { }

        protected virtual void OnUpdate(float deltaTime) { }

        internal void Enter()
        {
            if (Parent != null) Parent.ActiveChild = this;
            OnEnter();
            State init = GetInitialState();
            if (init != null) init.Enter();
        }

        internal void Exit()
        {
            if (ActiveChild != null){
                //Type type = ActiveChild.GetType();
                //string className = type.Name;
                //UnityEngine.Debug.Log(className);

                ActiveChild.Exit();
                
            }
            ActiveChild = null;
            OnExit();
        }

        internal void Update(float deltaTime)
        {
            State t = GetTransition();
            if (t != null)
            {
                if (isActiveChildExit&&ActiveChild!=null)
                {
                    Machine.Sequencer.RequestTransition(ActiveChild, t);
                }else{
                    Machine.Sequencer.RequestTransition(this, t);
                    isActiveChildExit = true;
                }
                return;
            }
            if (ActiveChild != null) ActiveChild.Update(deltaTime);
            OnUpdate(deltaTime);
        }

        public State Leaf()
        {
            State s = this;
            while (s.ActiveChild != null) s = s.ActiveChild;
            return s;
        }

        public IEnumerable<State> PathToRoot()
        {
            for (State s = this; s != null; s = s.Parent) yield return s;
        }
    }
}
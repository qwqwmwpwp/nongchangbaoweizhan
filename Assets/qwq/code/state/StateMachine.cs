using System.Collections.Generic;

namespace HSM
{
    public class StateMachine//状态机
    {
        public readonly State Root;
        public readonly TransitiomSequencer Sequencer;
        bool started;

        public StateMachine(State root)
        {
            Root = root;
            Sequencer = new TransitiomSequencer(this);
        }

        public void Start()
        {
            if (started) return;

            started = true;
            Root.Enter();
        }

        public void Tick(float deltaTime) {
            if (!started) Start();
            // InternalTick(deltaTime);
            Sequencer.Tick(deltaTime);
        }

        internal void InternalTick(float deltaTime) => Root.Update(deltaTime);

        //通过先退出到共享祖先节点，再进入目标节点，执行从 from 到 to 的实际切换。
        public void ChangeState(State from, State to)
        {
            if (from == to || from == null || to == null) return;

            State lca = TransitiomSequencer.Lca(from, to);
            // 当前分支从（但不包括）LCA出口
            for (State s = from; s != lca; s = s.Parent) s.Exit();
            //从LcA下到目标分支输入
            var stack = new Stack<State>();
            for (State s = to; s != lca; s = s.Parent) stack.Push(s);
            while (stack.Count > 0) stack.Pop().Enter();
        }
    }
}
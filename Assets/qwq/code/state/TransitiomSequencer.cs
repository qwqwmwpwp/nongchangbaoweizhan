using System;
using System.Collections.Generic;
using System.Threading;

namespace HSM
{
    public class TransitiomSequencer//状态过度序列器
    {
        public readonly StateMachine Machine;

        ISequence sequencer;          // 当前阶段（停用或激活）
        Action nextPhase;             // 阶段之间的切换结构
        (State from, State to)? pending; // 合并单个待处理请求
        State lastFrom, lastTo;

        public TransitiomSequencer (StateMachine machine)
        {
            Machine = machine;
        }

        // 请求从一个状态过渡到另一个状态
        public void RequestTransition(State from,State to) {
            // Machine.ChangeState(from, to);
            if (to == null || from == to) return;
            if (sequencer != null) { pending = (from, to); return; }
            BeginTransition(from, to);
        }

        static List<PhaseStep> GatherPhaseSteps(List<State> chain, bool deactivate)
        {
            var steps = new List<PhaseStep>();

            for (int i = 0; i < chain.Count; i++)
            {
                var acts = chain[i].Activities;

                for (int j = 0; j < acts.Count; j++)
                {
                    var a = acts[j];

                    if (deactivate)
                    {
                        if (a.Mode == ActivityMode.Active) steps.Add(ct => a.DeactivateAsync(ct));
                    }
                    else
                    {
                        if (a.Mode == ActivityMode.Inactive) steps.Add(ct => a.ActivateAsync(ct));
                    }
                }
            }
            return steps;
        }

        // States to exit: from → ... up to (but excluding) lca; bottom→up order.
        // 需要退出的状态：从起始状态（from）→ ... 到（但不包括）最低公共祖先（lca）；按自底向上的顺序。
        static List<State> StatesToExit(State from, State lca)
        {
            var list = new List<State>();
            for (var s = from; s != null && s != lca; s = s.Parent) list.Add(s);
            return list;
        }

        // States to enter: path from 'to' up to (but excluding) lca; returned in enter order (top->down).
        // 需要进入的状态：从目标状态（to）到（但不包括）最低公共祖先（lca）的路径；按进入顺序（自上而下）返回。
        static List<State> StatesToEnter(State to, State lca)
        {
            var stack = new Stack<State>();
            for (var s = to; s != lca; s = s.Parent) stack.Push(s);
            return new List<State>(stack);
        }

        CancellationTokenSource cts = new();
        public readonly bool UseSequential = true; // 设置为 false 以使用并行模式

        //状态过渡的执行器
        void BeginTransition(State from, State to)
        {
            var lca = Lca(from, to);
            var exitChain = StatesToExit(from, lca);
            var enterChain = StatesToEnter(to, lca);

            // 1. 停用“旧分支”
            //sequencer = new NoopPhase();
            var exitSteps = GatherPhaseSteps(exitChain, deactivate: true);
            sequencer = UseSequential
                ? new SequentialPhase(exitSteps, cts.Token)
                : new ParallelPhase(exitSteps, cts.Token);
            sequencer.Start();

            nextPhase = () =>
            {
                // 2. 切换状态
                Machine.ChangeState(from, to);
                // 3. 激活“新分支”
                // sequencer = new NoopPhase();
                var enterSteps = GatherPhaseSteps(enterChain, deactivate: false);
                sequencer = UseSequential
                    ? new SequentialPhase(enterSteps, cts.Token)
                    : new ParallelPhase(enterSteps, cts.Token);
                sequencer.Start();
            };
        }

        void EndTransition()
        {
            sequencer = null;

            if (pending.HasValue)
            {
                (State from, State to) p = pending.Value;
                pending = null;
                BeginTransition(p.from, p.to);
            }
        }

        //协调状态过渡和常规更新的执行顺序
        public void Tick(float deltaTime)
        {
            if (sequencer != null)
            {
                if (sequencer.Update())
                {
                    if (nextPhase != null)
                    {
                        var n = nextPhase;
                        nextPhase = null;
                        n();
                    }
                    else
                    {
                        EndTransition();
                    }
                }
                return; // 转换期间，我们不运行常规更新
            }
            Machine.InternalTick(deltaTime);
        }

        // 计算两个状态的最近公共祖先（LCA）。
        public static State Lca(State a,State b)
        {
            var ap = new HashSet<State>();
            for (var s = a; s != null; s = s.Parent) ap.Add(s);
            for (var s = b; s != null; s = s.Parent)
                if (ap.Contains(s)) return s;

            // 如果找不到共同祖先，请返回
            return null;
        }

    }


}
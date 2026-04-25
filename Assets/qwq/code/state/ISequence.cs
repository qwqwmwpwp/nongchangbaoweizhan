using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HSM
{
    //转换工作单元接口
    public interface ISequence
    {
        bool IsDone { get; }
        void Start();
        bool Update();
    }

    // 此阶段要运行的一个活动操作（激活 或 停用）。
    public delegate Task PhaseStep(CancellationToken ct);

    public class ParallelPhase : ISequence
    {
        readonly List<PhaseStep> steps;
        readonly CancellationToken ct;
        List<Task> tasks;
        public bool IsDone { get; private set; }

        public ParallelPhase(List<PhaseStep> steps, CancellationToken ct)
        {
            this.steps = steps;
            this.ct = ct;
        }

        public void Start()
        {
            if (steps == null || steps.Count == 0) { IsDone = true; return; }
            tasks = new List<Task>(steps.Count);
            for (int i = 0; i < steps.Count; i++) tasks.Add(steps[i](ct));
        }

        public bool Update()
        {
            if (IsDone) return true;
            IsDone = tasks == null || tasks.TrueForAll(t => t.IsCompleted);
            return IsDone;
        }
    }

    public class SequentialPhase : ISequence
    {
        readonly List<PhaseStep> steps;
        readonly CancellationToken ct;
        int index = -1;
        Task current;
        public bool IsDone { get; private set; }

        public SequentialPhase(List<PhaseStep> steps, CancellationToken ct)
        {
            this.steps = steps;
            this.ct = ct;
        }

        public void Start() => Next();

        public bool Update()
        {
            if (IsDone) return true;
            if (current == null || current.IsCompleted) Next();
            return IsDone; // 补充返回值，符合方法签名
        }

        void Next()
        {
            index++;
            if (index >= steps.Count) { IsDone = true; return; }
            current = steps[index](ct);
        }
    }

    public class NoopPhase : ISequence
    {
        public bool IsDone { get; private set; }
        public void Start() => IsDone = true; // 立即完成
        public bool Update() => IsDone;
    }

}
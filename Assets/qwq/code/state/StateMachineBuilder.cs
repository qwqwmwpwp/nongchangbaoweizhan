using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;

namespace HSM
{
    //状态机自动装配器，自动连接状态树并建立状态机与状态之间的双向引用。
    public class StateMachineBuilder
    {
        readonly State root;

        public StateMachineBuilder(State root)
        {
            this.root = root;
        }
        public StateMachine Build()
        {
            var m = new StateMachine(root);
            Wire(root, m, new HashSet<State>());
            return m;
        }
        void Wire(State s, StateMachine m, HashSet<State> visited)
        {
            if (s == null) return;
            if (!visited.Add(s)) return; //此状态已连线，避免重复处理

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            var machineField = typeof(State).GetField("Machine", flags);
            if (machineField != null) machineField.SetValue(s, m);

            foreach (var fld in s.GetType().GetFields(flags))
            {
                if (!typeof(State).IsAssignableFrom(fld.FieldType)) continue; // 只处理类型为 State 的字段
                if (fld.Name == "Parent") continue; // 确保子节点的父节点确实指向当前节点

                var child = (State)fld.GetValue(s);
                if (child == null) continue;
                if (!ReferenceEquals(child.Parent, s)) continue;//Ensure it ' s actually our direct child

                Wire(child, m, visited); // 递归处理子节点
            }
        }
    }
}
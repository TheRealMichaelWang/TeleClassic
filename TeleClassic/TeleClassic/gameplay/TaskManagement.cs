using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.gameplay
{
    public class Task : IComparable
    {
        public readonly TaskDelegate Delegate;
        public readonly byte Priority;
        readonly object[] arguments;

        public Task(TaskDelegate taskDelegate, byte priority)
        {
            this.Delegate = taskDelegate;
            this.Priority = priority;
        }

        public Task(TaskDelegate taskDelegate, byte priority, object[] arguments)
        {
            this.Delegate = taskDelegate;
            this.Priority = priority;
            this.arguments = arguments;
        }

        public void Invoke()
        {
            Delegate.Invoke(arguments);
        }

        public void Invoke(object[] args)
        {
            Delegate.Invoke(args);
        }

        public int CompareTo(object obj)
        {
            Task compare = obj as Task;
            return Priority.CompareTo(compare.Priority);
        }
    }

    public delegate void TaskDelegate(object[] args);
}

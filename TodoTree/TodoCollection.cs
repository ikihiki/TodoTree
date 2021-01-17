using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoTree
{
    public class TodoCollection
    {
        private List<Todo> todos = new List<Todo>();
        public IReadOnlyList<Todo> Todos => todos.AsReadOnly();
        public bool Empty => todos.Count == 0;

        public TodoCollection(IEnumerable<Todo> items)
        {
            todos = new List<Todo>(items);
        }

        public void Add(Todo todo)
        {
            todos.Add(todo);
        }

        public void Remove(Todo todo)
        {
            todos.Remove(todo);
        }
        
        public void Start()
        {
            todos.Find(todo=>!todo.Compleated)?.Start();
        }

        public void Stop()
        {
            foreach (var todo in todos)
            {
                todo.Stop();
            }
        }

        public void GoNext()
        {
            var currentIndex = todos.FindIndex(todo => !todo.Compleated);
            if (currentIndex < 0)
            {
                return;
            }

            todos[currentIndex].Compleated = true;
            var nextIndex = currentIndex + 1;
            if (nextIndex < todos.Count)
            {
                todos[nextIndex].Start();
            }
        }

        public bool GetCompleted()
        {
            return todos.All(todo => todo.Compleated);
        }

        public TimeSpan GetEstimateTime()
        {
            return todos.Aggregate(TimeSpan.Zero, (span, todo) => span + todo.EstimateTime);
        }

        public bool GetIsRunning()
        {
            return todos.Any(todo => todo.IsRunning);
        }

        public void Complete()
        {
            foreach (var todo in todos)
            {
                todo.Complete();
            }
        }

        internal void UnComplete()
        {
            foreach (var todo in todos)
            {
                todo.UnComplete();
            }
        }
    }
}

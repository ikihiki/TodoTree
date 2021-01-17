using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoTree
{
    public class Todo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        private TimeSpan estimateTime;
        public TimeSpan EstimateTime
        {
            get => todoCollection?.GetEstimateTime() ?? estimateTime;
            set => estimateTime = value;
        }
        public TimeSpan ElapsedTime => todoCollection?.GetEstimateTime() ?? timeRecords.GetEstimateTime();
        public TimeSpan RemainingTime => EstimateTime - ElapsedTime;
        private bool compleated;

        public bool Compleated
        {
            get => todoCollection?.GetCompleted() ?? compleated;
            set => compleated = value;
        }
        private TodoCollection todoCollection;
        public IEnumerable<Todo> Children => todoCollection?.Todos;

        private TimeRecordCollection timeRecords;
        public IEnumerable<TimeRecord> TimeRecords => timeRecords?.TimeRecords;
        public bool HasChildren => todoCollection != null;

        public bool IsRunning => HasChildren ? todoCollection.GetIsRunning() : timeRecords.IsRunning;

        public bool IsChild { get; set; }

        public Todo()
        {
            timeRecords = new TimeRecordCollection();
        }

        public Todo(string name, TimeSpan estimateTime, IEnumerable<TimeRecord> timeRecords)
        {
            this.timeRecords = new TimeRecordCollection(timeRecords);
            Name = name;
            EstimateTime = estimateTime;
        }

        public Todo(string name, IEnumerable<Todo> children)
        {
            Name = name;
            todoCollection = new TodoCollection(children);
        }


        public void Start()
        {
            if (HasChildren)
            {
                todoCollection.Start();
            }
            else
            {
                timeRecords.Start();

            }

        }

        public void Stop()
        {
            if (HasChildren)
            {
                todoCollection.Stop();
            }
            else
            {
                timeRecords.Stop();
            }

        }


        public void Complete()
        {
            Stop();
            if (HasChildren)
            {
                todoCollection.Complete();
            }
            else
            {
                compleated = true;
            }
        }

        public void UnComplete()
        {
            if (HasChildren)
            {
                todoCollection.UnComplete();
            }
            else
            {
                compleated = false;
            }
        }

        public void AddChild()
        {
            if (HasChildren)
            {
                todoCollection.Add(new Todo("New Todo", TimeSpan.Zero, Enumerable.Empty<TimeRecord>()) { IsChild = true });
            }
            else
            {
                todoCollection = new TodoCollection(new[]
                    {new Todo("New Todo", estimateTime, Enumerable.Empty<TimeRecord>()){IsChild = true}});
                estimateTime = TimeSpan.Zero;
                compleated = false;
                timeRecords = null;
            }
        }

        public void DeleteChild(Todo child)
        {
            if (!HasChildren)
            {
                return;

            }
            todoCollection.Remove(child);
            if (todoCollection.Empty)
            {
                estimateTime = todoCollection.GetEstimateTime();
                timeRecords = new TimeRecordCollection();
                todoCollection = null;

            }
        }

        public void DeleteAllChildren()
        {
            if (!HasChildren)
            {
                return;
            }

            estimateTime = todoCollection.GetEstimateTime();
            timeRecords = new TimeRecordCollection();
            todoCollection = null;
        }
    }
}

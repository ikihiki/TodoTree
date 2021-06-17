using System;
using System.Collections.Generic;
using LiteDB;
using Terminal.Gui;

namespace TodoTree.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var repository = new TodoRepository();
            repository.Add(new TodoDto() { Childrens = new List<ObjectId>(), Completed = false, Name = "Test", EstimateTime = TimeSpan.FromMinutes(1), Id = ObjectId.NewObjectId() });

            Application.Init();
            var top = App1.Body("tst",repository.GetAllTodo());
            var state = top.Create();
            Application.Top.Add(state.GetView());

            void OnStateOnRequestUpdate()
            {
                top.Update(state);
                Application.Refresh();
            }

            state.RequestUpdate += OnStateOnRequestUpdate;
            // Create a timer with a two second interval.
            var aTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += (sender, eventArgs) => OnStateOnRequestUpdate();
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            Application.Run();
    }
    }
}

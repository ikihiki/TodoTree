using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;
using Terminal.Gui;
using TodoTree.PluginInfra;

namespace TodoTree.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var repository = new TodoServiceClient("https://localhost:5001");
            await repository.Connect();

            Application.Init();
            var top = App1.Body("tst", repository);
            var state = top.Create();
            Application.Top.Add(state.GetView());

            void OnStateOnRequestUpdate()
            {
                top.Update(state);
                Application.Refresh();
            }

            state.RequestUpdate += OnStateOnRequestUpdate;
            repository.ChangeTodo += OnStateOnRequestUpdate;
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

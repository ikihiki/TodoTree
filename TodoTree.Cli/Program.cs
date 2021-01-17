using System;
using System.Collections.Generic;
using LiteDB;

namespace TodoTree.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var repository = new TodoRepository();
            repository.Add(new TodoDto(){Childrens = new List<ObjectId>(), Completed = false, Name = "Test", EstimateTime = TimeSpan.FromMinutes(1), Id = ObjectId.NewObjectId()});
            foreach (var todoDto in repository.GetAllTodo())
            {
                Console.WriteLine($"id: {todoDto.Id}, name: {todoDto.Name}, estimateTime: {todoDto.EstimateTime}, completed: {todoDto.Compleated}");
            }
        }
    }
}

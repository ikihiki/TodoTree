using System;
using System.Collections.Generic;
using System.Linq;
using TodoTree;
using TodoTree.PluginInfra;
using Xunit;

namespace TestProject1
{
    public class UnitTest1
    {
        [Fact]
        public void SingleAndNoParent()
        {
            var data = new TodoData()
            {
                Id = "1",
                Parent = null,
                Attributes = new Dictionary<string, string>(),
                EstimateTime = TimeSpan.FromSeconds(1),
                Name = "TestTodo",
                TimeRecords = new[]
                {
                    new TimeRecord(
                        new DateTime(2014, 02, 03, 11, 22, 33),
                        new DateTime(2014, 02, 03, 11, 22, 34)
                        )
                },
                Completed = true
            };

            var manager = new TodoManager();
            manager.UpsertTodo(data);

            var result = manager.TopTodo.First();

            Assert.Equal("1", result.Id);
        }

        [Fact]
        public void ParentAndChild()
        {
            var child = new TodoData()
            {
                Id = "2",
                Parent = "1",
                Attributes = new Dictionary<string, string>(),
                EstimateTime = TimeSpan.FromSeconds(1),
                Name = "TestTodo",
                TimeRecords = new[]
                {
                    new TimeRecord(
                        new DateTime(2014, 02, 03, 11, 22, 33),
                        new DateTime(2014, 02, 03, 11, 22, 34)
                    )
                },
                Completed = true
            };

            var parent = new TodoData()
            {
                Id = "1",
                Parent = null,
                Attributes = new Dictionary<string, string>(),
                EstimateTime = TimeSpan.FromSeconds(1),
                Name = "TestTodo",
                TimeRecords = new[]
                {
                    new TimeRecord(
                        new DateTime(2014, 02, 03, 11, 22, 33),
                        new DateTime(2014, 02, 03, 11, 22, 34)
                    )
                },
                Completed = true
            };

            var manager = new TodoManager();
            manager.UpsertTodo(parent);
            manager.UpsertTodo(child);

            var result = manager.TopTodo.First();

            Assert.Equal("1", result.Id);
            Assert.True(result.HasChildren);
            Assert.Equal("2", result.Children.First().Id);
        }

        [Fact]
        public void ChildAndParent()
        {
            var child = new TodoData()
            {
                Id = "2",
                Parent = "1",
                Attributes = new Dictionary<string, string>(),
                EstimateTime = TimeSpan.FromSeconds(1),
                Name = "TestTodo",
                TimeRecords = new[]
                {
                    new TimeRecord(
                        new DateTime(2014, 02, 03, 11, 22, 33),
                        new DateTime(2014, 02, 03, 11, 22, 34)
                    )
                },
                Completed = true
            };

            var parent = new TodoData()
            {
                Id = "1",
                Parent = null,
                Attributes = new Dictionary<string, string>(),
                EstimateTime = TimeSpan.FromSeconds(1),
                Name = "TestTodo",
                TimeRecords = new[]
                {
                    new TimeRecord(
                        new DateTime(2014, 02, 03, 11, 22, 33),
                        new DateTime(2014, 02, 03, 11, 22, 34)
                    )
                },
                Completed = true
            };

            var manager = new TodoManager();
            manager.UpsertTodo(child);
            manager.UpsertTodo(parent);

            var result = manager.TopTodo.First();

            Assert.Equal("1", result.Id);
            Assert.True(result.HasChildren);
            Assert.Equal("2", result.Children.First().Id);
        }

        [Fact]
        public void UpdateSingle()
        {
            var data = new TodoData()
            {
                Id = "1",
                Parent = null,
                Attributes = new Dictionary<string, string>(),
                EstimateTime = TimeSpan.FromSeconds(1),
                Name = "TestTodo",
                TimeRecords = new[]
                {
                    new TimeRecord(
                        new DateTime(2014, 02, 03, 11, 22, 33),
                        new DateTime(2014, 02, 03, 11, 22, 34)
                    )
                },
                Completed = true
            };

            var manager = new TodoManager();
            manager.UpsertTodo(data);
            data.Name = "Updated";
            manager.UpsertTodo(data);

            var result = manager.TopTodo.First();

            Assert.Equal("1", result.Id);
            Assert.Equal("Updated", result.Name);
        }
    }
}

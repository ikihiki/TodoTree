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
                    new TimeRecordData{
                        Start = new DateTime(2014, 02, 03, 11, 22, 33),
                        End = new DateTime(2014, 02, 03, 11, 22, 34)
                        }
                },
                Completed = true
            };

            var manager = new TodoManager();
            var change = manager.UpsertTodo(data);

            var result = manager.TopTodo.First();
            Assert.Equal(new[] { "1" }, change.OrderBy(id => id));
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
                    new TimeRecordData{
                        Start = new DateTime(2014, 02, 03, 11, 22, 33),
                        End = new DateTime(2014, 02, 03, 11, 22, 34)
                    }
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
                    new TimeRecordData{
                        Start = new DateTime(2014, 02, 03, 11, 22, 33),
                        End = new DateTime(2014, 02, 03, 11, 22, 34)
                    }
                },
                Completed = true
            };

            var manager = new TodoManager();
            manager.UpsertTodo(parent);
            var change = manager.UpsertTodo(child);

            var result = manager.TopTodo.First();
            Assert.Equal(new[] { "1","2" }, change.OrderBy(id => id));
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
                    new TimeRecordData{
                        Start = new DateTime(2014, 02, 03, 11, 22, 33),
                        End = new DateTime(2014, 02, 03, 11, 22, 34)
                    }
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
                    new TimeRecordData{
                        Start = new DateTime(2014, 02, 03, 11, 22, 33),
                        End = new DateTime(2014, 02, 03, 11, 22, 34)
                    }
                },
                Completed = true
            };

            var manager = new TodoManager();
            var change1 = manager.UpsertTodo(child);
            var change2 = manager.UpsertTodo(parent);

            var result = manager.TopTodo.First();
            Assert.Equal(new[] { "1", "2" }, change1.OrderBy(id => id));
            Assert.Equal(new[] { "1" }, change2.OrderBy(id => id));

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
                    new TimeRecordData{
                        Start = new DateTime(2014, 02, 03, 11, 22, 33),
                        End = new DateTime(2014, 02, 03, 11, 22, 34)
                    }
                },
                Completed = true
            };

            var manager = new TodoManager();
            manager.UpsertTodo(data);
            data.Name = "Updated";
            var change = manager.UpsertTodo(data);

            var result = manager.TopTodo.First();
            Assert.Equal(new[] { "1" }, change.OrderBy(id => id));

            Assert.Equal("1", result.Id);
            Assert.Equal("Updated", result.Name);
        }


        [Fact]
        public void UpdateParent()
        {
            var parent1 = new TodoData()
            {
                Id = "1",
                Parent = null,
                Attributes = new Dictionary<string, string>(),
                EstimateTime = TimeSpan.FromSeconds(1),
                Name = "TestTodo",
                TimeRecords = new[]
                {
                    new TimeRecordData{
                        Start = new DateTime(2014, 02, 03, 11, 22, 33),
                        End = new DateTime(2014, 02, 03, 11, 22, 34)
                    }
                },
                Completed = true
            };

            var parent2 = new TodoData()
            {
                Id = "2",
                Parent = null,
                Attributes = new Dictionary<string, string>(),
                EstimateTime = TimeSpan.FromSeconds(1),
                Name = "TestTodo",
                TimeRecords = new[]
                {
                    new TimeRecordData{
                        Start = new DateTime(2014, 02, 03, 11, 22, 33),
                        End = new DateTime(2014, 02, 03, 11, 22, 34)
                    }
                },
                Completed = true
            };

            var children = new TodoData()
            {
                Id = "3",
                Parent = "1",
                Attributes = new Dictionary<string, string>(),
                EstimateTime = TimeSpan.FromSeconds(1),
                Name = "Children",
                TimeRecords = new[]
                {
                    new TimeRecordData{
                        Start = new DateTime(2014, 02, 03, 11, 22, 33),
                        End = new DateTime(2014, 02, 03, 11, 22, 34)
                    }
                },
                Completed = true
            };
            var manager = new TodoManager();
            manager.UpsertTodo(parent1);
            manager.UpsertTodo(parent2);
            manager.UpsertTodo(children);
            children.Parent = "2";

            var change = manager.UpsertTodo(children);
            var result = manager.TopTodo.Single(t => t.Id == "2").Children.First();

            Assert.Equal(new[]{"1","2","3"}, change.OrderBy(id => id));
            Assert.Equal("3", result.Id);
        }

        [Fact]
        public void ParentToChild()
        {
            var parent1 = new TodoData()
            {
                Id = "1",
                Parent = null,
                Attributes = new Dictionary<string, string>(),
                EstimateTime = TimeSpan.FromSeconds(1),
                Name = "TestTodo",
                TimeRecords = new[]
                {
                    new TimeRecordData{
                        Start = new DateTime(2014, 02, 03, 11, 22, 33),
                        End = new DateTime(2014, 02, 03, 11, 22, 34)
                    }
                },
                Completed = true
            };

            var parent2 = new TodoData()
            {
                Id = "2",
                Parent = null,
                Attributes = new Dictionary<string, string>(),
                EstimateTime = TimeSpan.FromSeconds(1),
                Name = "TestTodo",
                TimeRecords = new[]
                {
                    new TimeRecordData{
                        Start = new DateTime(2014, 02, 03, 11, 22, 33),
                        End = new DateTime(2014, 02, 03, 11, 22, 34)
                    }
                },
                Completed = true
            };

            var manager = new TodoManager();
            manager.UpsertTodo(parent1);
            manager.UpsertTodo(parent2);
            parent2.Parent = "1";
            var change = manager.UpsertTodo(parent2);

            var result = manager.TopTodo.First().Children.First();
            Assert.Equal(new[] { "1","2" }, change.OrderBy(id=>id));

            Assert.Equal("2", result.Id);
        }
    }
}

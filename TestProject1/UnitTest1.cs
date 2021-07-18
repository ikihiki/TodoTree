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
            Assert.Equal(new[] { "1" }, change.Upsert.Select(t=>t.Id).OrderBy(id => id));
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
            Assert.Equal(new[] { "1", "2" }, change.Upsert.Select(t => t.Id).OrderBy(id => id));
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
            Assert.Equal(new[] { "1", "2" }, change1.Upsert.Select(t => t.Id).OrderBy(id => id));
            Assert.Equal(new[] { "1" }, change2.Upsert.Select(t => t.Id).OrderBy(id => id));

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
            Assert.Equal(new[] { "1" }, change.Upsert.Select(t => t.Id).OrderBy(id => id));

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

            Assert.Equal(new[] { "1", "2", "3" }, change.Upsert.Select(t => t.Id).OrderBy(id => id));
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
            Assert.Equal(new[] { "1", "2" }, change.Upsert.Select(t => t.Id).OrderBy(id => id));

            Assert.Equal("2", result.Id);
        }

        [Fact]
        public void DeleteSingleAndNoParent()
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
            var change = manager.DeleteTodo(data.Id);

            Assert.Equal(new[] { "1" }, change.Delete.Select(t => t.Id).OrderBy(id => id));
            Assert.Empty(manager.TopTodo);
        }

        [Fact]
        public void DeleteChild()
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
            manager.UpsertTodo(child);
            var change = manager.DeleteTodo(child.Id);

            var result = manager.TopTodo.First();
            Assert.Equal(new[] { "1" }, change.Upsert.Select(t => t.Id).OrderBy(id => id));
            Assert.Equal(new[] { "2" }, change.Delete.Select(t => t.Id).OrderBy(id => id));
            Assert.Equal("1", result.Id);
            Assert.False(result.HasChildren);
        }

        [Fact]
        public void DeleteParent()
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
            manager.UpsertTodo(child);
            var change = manager.DeleteTodo(parent.Id);

            Assert.Equal(new[] { "1", "2" }, change.Delete.Select(t => t.Id).OrderBy(id => id));
            Assert.Empty(manager.TopTodo);

        }

        [Fact]
        public void DeleteParent2()
        {

            var grandchild = new TodoData()
            {
                Id = "3",
                Parent = "2",
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
            manager.UpsertTodo(child);
            manager.UpsertTodo(grandchild);
            var change = manager.DeleteTodo(parent.Id);

            Assert.Equal(new[] { "1", "2", "3" }, change.Delete.Select(t => t.Id).OrderBy(id => id));
            Assert.Empty(manager.TopTodo);
        }

        [Fact]
        public void DeleteMulti()
        {
            var data1 = new TodoData()
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

            var data2 = new TodoData()
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
            manager.UpsertTodo(data1);
            manager.UpsertTodo(data2);
            var change = manager.DeleteTodo(new[] { data1,data2 });

            Assert.Equal(new[] { "1", "2" }, change.Delete.Select(t => t.Id).OrderBy(id => id));
            Assert.Empty(manager.TopTodo);
        }

        [Fact]
        public void FantomChild()
        {
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
            var pt = manager.GetTodo(parent.Id);
            pt.AddChild();
            manager.UpsertTodoRange(TodoConvert.Convert(pt));
            Assert.Single(pt.Children);
        }


        [Fact]
        public void Test()
        {
            var man1 = new TodoManager();
            var man2 = new TodoManager();

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
            var c = man1.UpsertTodo(parent);
            man2.ApplyChange(c);

            var t = man1.TopTodo.First();
            t.AddChild();
            c = man2.UpsertTodoRange(TodoConvert.Convert(t));
            man1.ApplyChange(c);
            var tc = t.Children.First();
            tc.AddChild();
            c = man2.UpsertTodoRange(TodoConvert.Convert(tc));
            man1.ApplyChange(c);
            var tgc = tc.Children.First();

            tgc.Name = "Name2";
            c = man2.UpsertTodoRange(TodoConvert.Convert(tc));
            man1.ApplyChange(c);

            Assert.Equal("Name2",man1.TopTodo.First().Children.First().Children.First().Name);
            Assert.Equal(t, t.Children.First().Parent);
        }

        [Fact]
        public void Start()
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
           var changes = manager.UpsertTodo(data);
           var m2 = new TodoManager();
           m2.ApplyChange(changes);
           var todo = m2.GetTodo(data.Id);
            todo.Start();
            var change = manager.UpsertTodo(TodoConvert.ConvertSingle(todo));
            var result = manager.TopTodo.First();
            Assert.Equal(new[] { "1" }, change.Upsert.Select(t => t.Id).OrderBy(id => id));

            Assert.Equal("1", result.Id);
            Assert.Equal(2, result.TimeRecords.Count());
        }

        [Fact]
        public void AddGrandChild()
        {

            var grandchild = new TodoData()
            {
                Id = "3",
                Parent = "2",
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
            manager.UpsertTodo(grandchild);
            manager.UpsertTodo(child);
            manager.UpsertTodo(parent);
            
            Assert.Single(manager.TopTodo);
        }

    }


}

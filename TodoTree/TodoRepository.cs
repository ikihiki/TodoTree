﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace TodoTree
{
    public class TodoDto
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public TimeSpan EstimateTime { get; set; }
        public bool Completed { get; set; }
        public IList<TimeRecord> TimeRecords { get; set; }
        public IList<ObjectId> Childrens { get; set; }
        public bool IsChild { get; set; }
    }

    public class TodoRepository : IDisposable
    {
        private LiteDatabase db;

        public TodoRepository()
        {
            db = new LiteDatabase("todo.db");
        }

        public void Dispose()
        {
            db?.Dispose();
        }

        public void Add(TodoDto dto)
        {
            var todos = db.GetCollection<TodoDto>("todos");
            todos.Insert(dto);
        }

        public ObjectId AddOrUpdate(Todo todo)
        {
            var children = todo.Children?.Select(todo => AddOrUpdate(todo)).ToList();
            var dto = new TodoDto()
            {
                Childrens = children,
                Completed = children == null && todo.Compleated,
                EstimateTime = children == null ? todo.EstimateTime : TimeSpan.Zero,
                Name = todo.Name,
                TimeRecords = todo.TimeRecords?.ToList(),
                Id = todo.Id == null ? ObjectId.NewObjectId() : new ObjectId(todo.Id),
                IsChild = todo.IsChild
            };
            var todos = db.GetCollection<TodoDto>("todos");
            todos.Upsert(dto);
            return dto.Id;
        }

        public IEnumerable<TodoDto> GeTodos()
        {
            var todos = db.GetCollection<TodoDto>("todos");
            return todos.Query().ToEnumerable();
        }

        public Todo GeTodoById(ObjectId id)
        {
            var todos = db.GetCollection<TodoDto>("todos");
            var dto = todos.FindById(id);
            if (dto == null)
            {
                return null;
            }

            if (dto.Childrens == null)
            {
                return new Todo(dto.Name, dto.EstimateTime, dto.TimeRecords) { Compleated = dto.Completed, Id = dto.Id.ToString(), IsChild = dto.IsChild};
            }
            else
            {
                return new Todo(dto.Name, dto.Childrens.Select(id => GeTodoById(id)).Where(todo => todo != null)) { Id = dto.Id.ToString(), IsChild = dto.IsChild};
            }
        }

        public IEnumerable<Todo> GetTopTodo()
        {
            var todos = db.GetCollection<TodoDto>("todos");
            return todos.Query().Where(todo=>!todo.IsChild).Select(todo => todo.Id).ToEnumerable().Select(id => GeTodoById(id));
        }

        public IEnumerable<Todo> GetAllTodo()
        {
            var todos = db.GetCollection<TodoDto>("todos");
            return todos.Query().Select(todo => todo.Id).ToEnumerable().Select(id => GeTodoById(id));
        }

        public void Delete(Todo todo)
        {
            var todos = db.GetCollection<TodoDto>("todos");
            todos.Delete(todo.Id);
        }
    }
}
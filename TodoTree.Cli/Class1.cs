using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using NStack;
using Terminal.Gui;
using TodoTree.PluginInfra;
using static TodoTree.Cli.UI;

namespace TodoTree.Cli
{
    public static class App1
    {
        public static Element Body(string txt, TodoServiceClient repository)
        {
            return CreateElement((state) =>
            {
                var (count, setCount) = state.CreateState(0);
                var (showAddWindow, setShowAddWindow) = state.CreateState(false);
                var (showAll, setShowAll) = state.CreateState(false);
                IEnumerable<Todo> GetTodos(bool showAll)
                {
                    return repository.GetTopTodos().Where(todo => showAll || !todo.Compleated).ToArray();
                }

                var (selectedTodo, setSelectedTodo) = state.CreateState<Todo>(null);
                var builder = new DelegateTreeBuilder<Todo>(todo => showAll ? todo.Children : todo.UncompletedChildren);
                return
                    Container(height: Dim.Fill(), width: Dim.Fill(), contents: new[]
                    {
                        VStack(height: Dim.Fill(), width: Dim.Fill(), contents: new[]
                        {
                            HStack(height:Dim.Sized(1), width:Dim.Fill(),contents:new []
                            {
                                Button("ルート追加", width:Dim.Sized(10), click:()=>setShowAddWindow(true)),
                                Button("子追加", width:Dim.Sized(10), click:()=>setShowAddWindow(true)),
                                Button("削除", width:Dim.Sized(10), click:()=>setShowAddWindow(true)),
                                Button(selectedTodo?.Compleated == true ? "未完了にする": "完了にする", width:Dim.Sized(10), click:()=>
                                {
                                    if(selectedTodo == null)
                                    {
                                        return;
                                    }
                                    if(selectedTodo.Compleated)
                                    {
                                        selectedTodo.UnComplete();
                                    }
                                    else
                                    {
                                        selectedTodo.Complete();
                                    }
                                    _ = repository.Upsert(selectedTodo);
                                }),
                                CheckBox("すべて表示", showAll, val=>
                                {
                                    setShowAll(val);
                                }),
                            }),
                            TreeView(
                                GetTodos(showAll).ToArray(),
                                static render =>$"{(render.IsRunning ? '▶' : '⏸')}  {(render.Compleated ? '✔' : '□')} {render.EstimateTime} - { render.RemainingTime } - { render.Name }",
                                builder,
                                equalityComparer: TodoIdEqualityComparer.Instance,
                                objectActivated: (todo)=>
                                {
                                    if(todo.IsRunning)
                                    {
                                        todo.Stop();
                                    }
                                    else
                                    {
                                        todo.Start();
                                    }
                                    _ = repository.Upsert(todo);

                                },
                                selected: (todo) =>
                                {
                                    setSelectedTodo(todo);
                                },
                                x:Pos.At(3),
                                width:Dim.Fill(),
                                height:Dim.Fill()
                            ),
                        }),
                        showAddWindow?
                            Window(
                                title:"追加",
                                content: AddWindow(
                                    cancel:()=>setShowAddWindow(false),
                                    ok:(title, time)=>
                                    {
                                        setShowAddWindow(false);
                                        _ = repository.Upsert(new Todo(title, time, Enumerable.Empty<TimeRecord>(), new Dictionary<string,string>()));
                                    }
                                ),
                                x:Pos.At(2),
                                y:Pos.At(2),
                                height: Dim.Percent(90),
                                width:Dim.Percent(90)
                            )
                            :null
                    });
            });
        }

        public static Element AddWindow(Action cancel, Action<string, TimeSpan> ok)
        {
            return CreateElement(state =>
            {
                var (title, setTitle) = state.CreateState("");
                var (time, setTime) = state.CreateState(TimeSpan.Zero);
                return VStack(height: Dim.Fill(), width: Dim.Fill(), contents: new[]
                {
                    Label("タイトル", height:Dim.Sized(1)),
                    TextField(title, setTitle,height:Dim.Sized(1)),
                    Label("想定時間", height:Dim.Sized(1)),
                    TimeField(time, setTime,height:Dim.Sized(1)),
                    Label($"{time} {title}",height:Dim.Sized(1)),
                    HStack(height:Dim.Sized(1), width:Dim.Fill(),contents:new []
                    {
                        Button("OK", width:Dim.Sized(5), click:()=>ok(title, time)),
                        Button($"Cancel",click: ()=> cancel()),
                    }),
                });
            });
        }
    }






    public static partial class UI
    {
        public static Element CreateElement(Func<CustomElement.CustomElementState, Element> func)
        {
            return new CustomElement()
            {
                Func = func
            };
        }

        public static Element Button(string text, Action click = null, Pos x = null, Pos y = null, Dim width = null, Dim height = null)
        {
            return new ButtonElement()
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Text = text,
                Click = click
            };
        }


        public static Element Container(Element[] contents, Pos x = null, Pos y = null, Dim width = null, Dim height = null)
        {
            return new ContainerElement()
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,

                Elements = contents
            };
        }

        public static Element VStack(Element[] contents, Pos x = null, Pos y = null, Dim width = null, Dim height = null)
        {
            return new VStackElement()
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Elements = contents
            };
        }

        public static Element HStack(Element[] contents, Pos x = null, Pos y = null, Dim width = null, Dim height = null)
        {
            return new HStackElement()
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Elements = contents
            };
        }

        public static Element Window(Element content, string title = null, Pos x = null, Pos y = null, Dim width = null, Dim height = null)
        {
            return new WindowElement
            {
                Content = content,
                Title = title,
                X = x,
                Y = y,
                Width = width,
                Height = height
            };
        }

        public static Element Label(string text, Pos x = null, Pos y = null, Dim width = null, Dim height = null)
        {
            return new LabelElement()
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Text = text,
            };
        }

        public static Element TextField(string text, Action<string> textChanged = null, Pos x = null, Pos y = null, Dim width = null, Dim height = null)
        {
            return new TextFieldElement()
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Text = text,
                TextChanged = textChanged
            };
        }
    }
    public abstract class ElementState
    {
        public event Action RequestUpdate;

        public abstract View GetView();

        protected void RiseRequestUpdate()
        {
            RequestUpdate?.Invoke();
        }

    }

    public class ElementState<TElement, TView> : ElementState where TElement : Element where TView : View
    {
        public TElement Prev { get; set; }
        public TView View { get; set; }

        public ElementState(TElement element, TView view)
        {
            Prev = element;
            View = view;
            View.X = element.X;
            View.Y = element.Y;
            View.Width = element.Width;
            View.Height = element.Height;
        }

        public override View GetView()
        {
            return View;
        }

        public virtual bool Update(TElement element)
        {
            if (element.Width is not null && View.Width != element.Width)
            {
                View.Width = element.Width;
            }
            if (element.Y is not null && View.Height != element.Height)
            {
                View.Height = element.Height;
            }
            if (element.X is not null && View.X != element.X)
            {
                View.X = element.X;
            }
            if (element.Y is not null && View.Y != element.Y)
            {
                View.Y = element.Y;
            }

            Prev = element;
            return true;
        }
    }
    public abstract class Element
    {
        public Dim Width { get; set; }
        public Dim Height { get; set; }
        public Pos X { get; set; }
        public Pos Y { get; set; }
        public abstract ElementState Create();
        public abstract bool Update(ElementState state);
    }

    public class ButtonElement : Element
    {
        public class State : ElementState<ButtonElement, Button>
        {
            public State(ButtonElement element) : base(element, new Button(element.Text)) { }

            public override bool Update(ButtonElement element)
            {
                if (View.Text != element.Text)
                {
                    View.Text = element.Text;
                }

                return base.Update(element);
            }
        }

        public string Text { get; set; }

        public Action Click { get; set; }

        public override ElementState Create()
        {

            var state = new State(this);
            state.View.Clicked += () => state.Prev.Click?.Invoke();
            return state;
        }

        public override bool Update(ElementState state)
        {
            return state is State s && s.Update(this);
        }
    }

    public class ContainerElement : Element
    {

        public class ContainerElementState : ElementState<ContainerElement, View>
        {
            public List<ElementState> States { get; } = new();

            public ContainerElementState(ContainerElement element) : base(element, new View())
            {
                foreach (var child in element.Elements)
                {
                    if (child is not null)
                    {
                        Add(child);
                    }
                }
            }

            public override bool Update(ContainerElement element)
            {
                for (int i = 0; i < element.Elements.Length; i++)
                {
                    if (States.Count < i)
                    {
                        var newOne = element.Elements[i].Create();
                        View.Add(newOne.GetView());
                        States.Add(newOne);
                    }
                    else
                    {
                        if (i >= States.Count)
                        {
                            var newOne = element.Elements[i].Create();
                            View.Add(newOne.GetView());
                            States.Add(newOne);
                            States[i].RequestUpdate += RiseRequestUpdate;
                        }
                        else
                        {

                            var result = element.Elements[i].Update(States[i]);
                            if (!result)
                            {
                                View.Remove(States[i].GetView());
                                States[i].RequestUpdate -= RiseRequestUpdate;
                                var newOne = element.Elements[i].Create();
                                View.Add(newOne.GetView());
                                States[i] = newOne;
                                States[i].RequestUpdate += RiseRequestUpdate;
                            }
                        }
                    }

                }

                if (element.Elements.Length < States.Count)
                {

                    for (int i = element.Elements.Length; i < States.Count; i++)
                    {
                        View.Remove(States[i].GetView());
                        States[i].RequestUpdate -= RiseRequestUpdate;
                    }

                    States.RemoveRange(element.Elements.Length, States.Count - element.Elements.Length);
                }

                return base.Update(element);
            }


            public void Add(Element element)
            {
                var state = element.Create();
                States.Add(state);
                state.RequestUpdate += RiseRequestUpdate;
                View.Add(state.GetView());
            }
        }

        private Element[] elements;
        public Element[] Elements
        {
            get => elements;
            set => elements = value.Where(v => v is not null).ToArray();
        }
        public override ContainerElementState Create()
        {
            return new ContainerElementState(this);
        }

        public override bool Update(ElementState state)
        {
            return state is ContainerElementState s && s.Update(this);
        }
    }

    public class VStackElement : ContainerElement
    {
        public override ContainerElementState Create()
        {
            var state = base.Create();
            View prevView = null;
            foreach (var childState in state.States)
            {
                var view = childState.GetView();
                if (prevView is not null)
                {
                    view.Y = Pos.Bottom(prevView);
                }

                prevView = view;
            }
            return state;
        }

        public override bool Update(ElementState state)
        {
            var s = state as ContainerElementState;
            if (s is null)
            {
                return false;
            }

            var result = s.Update(this);
            if (!result)
            {
                return false;
            }

            View prevView = null;
            foreach (var childState in s.States)
            {
                var view = childState.GetView();
                if (prevView is not null)
                {
                    view.Y = Pos.Bottom(prevView);
                }

                prevView = view;
            }

            return true;
        }
    }

    public class HStackElement : ContainerElement
    {
        public override ContainerElementState Create()
        {
            var state = base.Create();
            View prevView = null;
            foreach (var childState in state.States)
            {
                var view = childState.GetView();
                if (prevView is not null)
                {
                    view.X = Pos.Right(prevView);
                }

                prevView = view;
            }

            return state;
        }

        public override bool Update(ElementState state)
        {
            var s = state as ContainerElementState;
            if (s is null)
            {
                return false;
            }

            var result = s.Update(this);
            if (!result)
            {
                return false;
            }

            View prevView = null;
            foreach (var childState in s.States)
            {
                var view = childState.GetView();
                if (prevView is not null)
                {
                    view.X = Pos.Right(prevView);
                }

                prevView = view;
            }

            return true;
        }
    }

    public class CustomElement : Element
    {

        public class CustomElementState : ElementState
        {
            public CustomElement Prev { get; set; }
            public ElementState ElementState { get; set; }
            public class StatePair
            {
                public object Value;
                public Delegate Updater;
            }

            public List<StatePair> StateList = new();
            public int StateCallCount = 0;
            public bool FirstTime = true;

            public void StartUpdate()
            {
                StateCallCount = 0;
            }

            public void EndUpdate()
            {
                FirstTime = false;
            }

            public (T, Action<T>) CreateState<T>(T initial)
            {
                if (FirstTime)
                {
                    var updater = CreateUpdater<T>(StateCallCount);
                    var tuple = new StatePair
                    {
                        Value = initial,
                        Updater = updater
                    };
                    StateList.Add(tuple);
                    StateCallCount++;
                    return (initial, updater);
                }
                else
                {
                    var tuple = StateList[StateCallCount];
                    StateCallCount++;
                    return ((T)tuple.Value, (Action<T>)tuple.Updater);
                }
            }

            private Action<T> CreateUpdater<T>(int number)
            {
                return value =>
                {
                    if (!EqualityComparer<T>.Default.Equals((T)StateList[number].Value, value))
                    {
                        StateList[number].Value = value;
                        RiseRequestUpdate();
                    }
                };
            }

            public CustomElementState(CustomElement element)
            {
                Prev = element;
                this.StartUpdate();
                ElementState = element.Func(this).Create();
                this.EndUpdate();
                ElementState.RequestUpdate += RiseRequestUpdate;
            }

            public bool Update(CustomElement element)
            {
                StartUpdate();
                var childElement = element.Func(this);
                EndUpdate();
                var result = childElement.Update(ElementState);
                if (!result)
                {
                    ElementState.RequestUpdate -= RiseRequestUpdate;
                    ElementState = childElement.Create();
                    ElementState.RequestUpdate += RiseRequestUpdate;
                }
                Prev = element;
                return true;
            }

            public override View GetView()
            {
                return ElementState.GetView();
            }

            internal void BindEvent(Action changeTodo)
            {
                throw new NotImplementedException();
            }
        }

        public Func<CustomElementState, Element> Func { get; set; }

        public override ElementState Create()
        {
            return new CustomElementState(this);
        }

        public override bool Update(ElementState state)
        {
            return state is CustomElementState s && s.Update(this);
        }
    }

    public class WindowElement : Element
    {
        public class State : ElementState<WindowElement, Window>
        {
            public ElementState ContentState { get; set; }

            public State(WindowElement element) : base(element, new Window())
            {
                View.Title = element.Title;
                ContentState = element.Content.Create();
                View.Add(ContentState.GetView());
                ContentState.RequestUpdate += RiseRequestUpdate;
            }

            public override bool Update(WindowElement element)
            {
                var result = element.Content.Update(ContentState);
                if (!result)
                {
                    ContentState.RequestUpdate -= RiseRequestUpdate;
                    ContentState = element.Content.Create();
                    View.RemoveAll();
                    View.Add(ContentState.GetView());
                    ContentState.RequestUpdate += RiseRequestUpdate;
                }

                if (View.Title != element.Title)
                {
                    View.Title = element.Title;
                }
                return base.Update(element);
            }
        }

        public string Title { get; set; }
        public Element Content { get; set; }

        public override ElementState Create()
        {
            return new WindowElement.State(this);
        }

        public override bool Update(ElementState state)
        {
            return state is State s && s.Update(this);
        }
    }


    public class LabelElement : Element
    {
        public class State : ElementState<LabelElement, Label>
        {
            public State(LabelElement element) : base(element, new Label(element.Text)) { }

            public override bool Update(LabelElement element)
            {
                if (View.Text != element.Text)
                {
                    View.Text = element.Text;
                }

                return base.Update(element);
            }
        }

        public string Text { get; set; }

        public override ElementState Create()
        {
            return new State(this);
        }

        public override bool Update(ElementState state)
        {
            return state is State s && s.Update(this);
        }
    }

    public class TextFieldElement : Element
    {
        public class State : ElementState<TextFieldElement, TextField>
        {
            public State(TextFieldElement element) : base(element, new TextField(element.Text))
            {
                View.TextChanged += (txt) => Prev.TextChanged?.Invoke(View.Text.ToString());
            }

            public override bool Update(TextFieldElement element)
            {
                if (!View.HasFocus && View.Text != element.Text)
                {
                    View.Text = element.Text;
                }

                return base.Update(element);
            }
        }

        public string Text { get; set; }

        public Action<string> TextChanged { get; set; }

        public override ElementState Create()
        {
            return new State(this);
        }

        public override bool Update(ElementState state)
        {
            return state is State s && s.Update(this);
        }
    }
}

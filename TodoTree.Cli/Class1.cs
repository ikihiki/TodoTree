using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Terminal.Gui;
using static TodoTree.Cli.UI;

namespace TodoTree.Cli
{
    public static class App1
    {
        public static Element Body(string txt, IEnumerable<Todo> todos)
        {
            var builder = new DelegateTreeBuilder<Todo>(static todo => todo.Children);
            return CreateElement((state) =>
            {
                var (count, setCount) = state.CreateState(0);
                return VStack(contents: new[]
                {
                    Button(txt),
                    Button($"button{count}",click: ()=> setCount(count+1)),
                    TreeView(todos.ToArray(),static render =>JsonSerializer.Serialize(render),  builder, x:Pos.At(3), width:Dim.Fill(), height:Dim.Fill())
                });
            });
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

        public override View GetView()
        {
            return View;
        }
    }



    public static class UI
    {
        public static Element CreateElement(Func<CustomElement.CustomElementState, Element> func)
        {
            return new CustomElement()
            {
                Func = func
            };
        }

        public static Element Button(string text, Pos x = null, Pos y = null, Dim width = null, Action click = null)
        {
            return new ButtonElement()
            {
                Text = text,
                X = x,
                Y = y,
                Width = width,
                Click = click
            };
        }


        public static Element Container(Element[] contents)
        {
            return new ContainerElement()
            {
                Elements = contents
            };
        }

        public static Element VStack(Element[] contents)
        {
            return new VStackElement()
            {
                Elements = contents
            };
        }

        public static Element TreeView<T>(T[] root, AspectGetterDelegate<T> aspectGetter, DelegateTreeBuilder<T> treeBuilder, Pos x = null, Pos y = null, Dim width = null, Dim height = null) where T : class
        {
            return new TreeViewElement<T>
            {
                Root = root,
                AspectGetter = aspectGetter,
                TreeBuilder = treeBuilder,
                X = x,
                Y = y,
                Width = width,
                Height = height
            };
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
        public string Text { get; set; }

        public Action Click { get; set; }


        public override ElementState Create()
        {
            var view = new Button(Text)
            {
                X = X,
                Y = Y,
                Width = Width,
            };

            var state = new ElementState<ButtonElement, Button>
            {
                Prev = this,
                View = view
            };
            view.Clicked += () => state.Prev.Click?.Invoke();
            return state;
        }

        public override bool Update(ElementState state)
        {
            var s = state as ElementState<ButtonElement, Button>;
            if (s is null)
            {
                return false;
            }

            if (s.Prev.Text != Text)
            {
                s.View.Text = Text;
            }

            if (s.Prev.Width != Width)
            {
                s.View.Width = Width;
            }
            if (s.Prev.X != X)
            {
                s.View.X = X;
            }
            if (s.Prev.Y != Y)
            {
                s.View.Y = Y;
            }
            s.Prev = this;
            return true;
        }
    }

    public class ContainerElement : Element
    {

        public class ContainerElementState : ElementState<ContainerElement, View>
        {
            public List<ElementState> States { get; } = new();

            public ContainerElementState(ContainerElement element)
            {
                Prev = element;
                View = new View();
                View.Height = Dim.Fill();
                View.Width = Dim.Fill();
                foreach (var child in element.Elements)
                {
                    Add(child);
                }
            }

            public bool Update(ContainerElement element)
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

                if (element.Elements.Length < States.Count)
                {

                    for (int i = element.Elements.Length; i < States.Count; i++)
                    {
                        View.Remove(States[i].GetView());
                        States[i].RequestUpdate -= RiseRequestUpdate;
                    }

                    States.RemoveRange(element.Elements.Length, States.Count - element.Elements.Length);
                }

                Prev = element;
                return true;
            }


            public void Add(Element element)
            {
                var state = element.Create();
                States.Add(state);
                state.RequestUpdate += RiseRequestUpdate;
                View.Add(state.GetView());
            }
        }

        public Element[] Elements { get; set; }
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
                    return ((T)tuple.Value, (Action<T>)tuple.Updater);
                }
            }

            private Action<T> CreateUpdater<T>(int number)
            {
                return value => { StateList[number].Value = value; RiseRequestUpdate(); };
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



    public class TreeViewElement<T> : Element where T : class
    {

        class State : ElementState<TreeViewElement<T>, TreeView<T>>
        {
            public State(TreeViewElement<T> element)
            {
                Prev = element;
                View = new TreeView<T>()
                {
                    AspectGetter = element.AspectGetter,
                    TreeBuilder = element.TreeBuilder,
                    X = element.X,
                    Y = element.Y,
                    Width = element.Width,
                    Height = element.Height
                };

                foreach (var value in element.Root)
                {
                    View.AddObject(value);
                }
            }

            internal bool Update(TreeViewElement<T> treeViewElement)
            {
                Prev = treeViewElement;
                if (View.AspectGetter != treeViewElement.AspectGetter)
                {
                    View.AspectGetter = treeViewElement.AspectGetter;
                }

                if (View.TreeBuilder != treeViewElement.TreeBuilder)
                {
                    View.TreeBuilder = treeViewElement.TreeBuilder;
                }
                if (treeViewElement.Width is not null && View.Width != treeViewElement.Width)
                {
                    View.Width = treeViewElement.Width;
                }
                if (treeViewElement.Y is not null && View.Height != treeViewElement.Height)
                {
                    View.Height = treeViewElement.Height;
                }
                if (treeViewElement.X is not null && View.X != treeViewElement.X)
                {
                    View.X = treeViewElement.X;
                }
                if (treeViewElement.Y is not null && View.Y != treeViewElement.Y)
                {
                    View.Y = treeViewElement.Y;
                }
                return true;
            }
        }

        public T[] Root { get; set; }
        public AspectGetterDelegate<T> AspectGetter { get; set; }
        public DelegateTreeBuilder<T> TreeBuilder { get; set; }

        public override ElementState Create()
        {
            return new State(this);
        }

        public override bool Update(ElementState state)
        {
            return state is TreeViewElement<T>.State s && s.Update(this);
        }
    }
}

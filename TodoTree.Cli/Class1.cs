using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using static ConsoleApp8.UI;

namespace ConsoleApp8
{
    public static class App1
    {
        public static Element Body(string txt)
        {
            return CreateElement((state) =>
            {
                var (count, setCount) = state.CreateState(0);



                return Container(contents: new[]
                {
                    Button(txt),
                    Button($"btn{count}", Pos.At(1), Pos.At(2), click: ()=> setCount(count+1))
                });
            });
        }
    }


    public class ElementState
    {
        public class StatePair
        {
            public object Value;
            public Delegate Updater;
        }

        public List<StatePair> StateList = new ();
        public int StateCallCount = 0;
        public bool FirstTime = true;
        public event Action RequestUpdate;

        public virtual View GetView()
        {
            return null;
        }

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
                return ((T) tuple.Value, (Action<T>) tuple.Updater);
            }
        }

        private Action<T> CreateUpdater<T>(int number)
        {
            return new Action<T>((T value) => {StateList[number].Value = value; RequestUpdate?.Invoke(); });
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
        public static Element CreateElement(Func<ElementState, Element> func)
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
    }

    public abstract class Element
    {
        public abstract ElementState Create();
        public abstract bool Update(ElementState state);

        public event Action RequestUpdate;

        public void RizeRequestUpdate()
        {
            RequestUpdate?.Invoke();
        }

    }

    public class ButtonElement : Element
    {
        public string Text { get; set; }
        public Dim Width { get; set; }
        public Pos X { get; set; }
        public Pos Y { get; set; }
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
            view.Clicked += () => state.Prev.Click();
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
        public Element[] Elements { get; set; }
        public List<ElementState> States { get; set; }


        public override ElementState Create()
        {
            var view = new View();
            view.AutoSize = true;
            view.Width = 8;
            view.Height = 4;
            States = new List<ElementState>();
            foreach (var element in Elements)
            {
                var state = element.Create();
                state.RequestUpdate += State_RequestUpdate;
                States.Add(state);
                view.Add(state.GetView());
            }
            return new ElementState<ContainerElement, View>
            {
                Prev = this,
                View = view
            };
        }

        private void State_RequestUpdate()
        {
            RizeRequestUpdate();
        }

        public override bool Update(ElementState state)
        {
            var s = state as ElementState<ContainerElement, View>;
            if (s is null)
            {
                return false;
            }

            var view = s.GetView();
            States = s.Prev.States;
            for (int i = 0; i < Elements.Length; i++)
            {
                if (s.Prev.States.Count < i)
                {
                    var newOne = Elements[i].Create();
                    view.Add(newOne.GetView());
                    States.Add(newOne);
                }
                else
                {
                    var result = Elements[i].Update(States[i]);
                    if (!result)
                    {
                        view.Remove(States[i].GetView());
                        States[i].RequestUpdate -= State_RequestUpdate;
                        var newOne = Elements[i].Create();
                        view.Add(newOne.GetView());
                        States[i] = newOne;
                        States[i].RequestUpdate += State_RequestUpdate;
                    }
                }
            }

            if (Elements.Length < States.Count)
            {

                for (int i = Elements.Length; i < States.Count; i++)
                {
                    view.Remove(States[i].GetView());
                    States[i].RequestUpdate -= State_RequestUpdate;
                }

                States.RemoveRange(Elements.Length, States.Count - Elements.Length);
            }

            s.Prev = this;
            return true;
        }
    }

    public class CustomElementState : ElementState
    {
        public CustomElement Prev { get; set; }
        public override View GetView()
        {
            return Prev.ElementState.GetView();
        }
    }

    public class CustomElement : Element
    {
        public Func<ElementState, Element> Func { get; set; }
        public ElementState State { get; set; }
        public ElementState ElementState { get; set; }
        public Element Element { get; set; }

        public override ElementState Create()
        {
            State = new ElementState();
            State.RequestUpdate += Element_RequestUpdate;
            State.StartUpdate();
            Element = Func(State);
            State.EndUpdate();
            Element.RequestUpdate += Element_RequestUpdate;
            ElementState = Element.Create();
            ElementState.RequestUpdate += Element_RequestUpdate;

            return new CustomElementState()
            {
                Prev = this
            };
        }

        private void Element_RequestUpdate()
        {
            RizeRequestUpdate();
        }

        public override bool Update(ElementState state)
        {
            var s = state as CustomElementState;
            if (s is null)
            {
                return false;
            }

            State = s.Prev.State;
            
            Element.RequestUpdate -= Element_RequestUpdate;
            State.StartUpdate();
            Element = Func(State);
            State.EndUpdate();
            Element.RequestUpdate += Element_RequestUpdate;
            var result = Element.Update(ElementState);
            if (!result)
            {
                ElementState.RequestUpdate -= Element_RequestUpdate;
                ElementState = Element.Create();
                ElementState.RequestUpdate += Element_RequestUpdate;
            }
            s.Prev = this;

            return true;
        }
    }
}

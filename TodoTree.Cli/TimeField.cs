using System;
using Terminal.Gui;

namespace TodoTree.Cli
{
    public class TimeFieldElement : Element
    {
        public class State : ElementState<TimeFieldElement, TimeField>
        {
            public State(TimeFieldElement element) : base(element, new TimeField(element.TimeSpan))
            {
                View.TimeChanged += (time) => Prev.TimeChanged?.Invoke(time.NewValue);
            }

            public override bool Update(TimeFieldElement element)
            {
                if (!View.HasFocus && View.Time != element.TimeSpan)
                {
                    View.Time = element.TimeSpan;
                }

                return base.Update(element);
            }
        }

        public TimeSpan TimeSpan { get; set; }

        public Action<TimeSpan> TimeChanged { get; set; }

        public override ElementState Create()
        {
            return new State(this);
        }

        public override bool Update(ElementState state)
        {
            return state is State s && s.Update(this);
        }
    }

    public static partial class UI
    {
        public static Element TimeField(TimeSpan time, Action<TimeSpan> timeChanged = null, Pos x = null, Pos y = null, Dim width = null, Dim height = null)
        {
            return new TimeFieldElement()
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                TimeSpan = time,
                TimeChanged = timeChanged
            };
        }
    }
}

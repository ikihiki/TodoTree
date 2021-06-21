using System;
using Terminal.Gui;

namespace TodoTree.Cli
{
    public class CheckBoxElement : Element
    {
        public class State : ElementState<CheckBoxElement, CheckBox>
        {
            public State(CheckBoxElement element) : base(element, new CheckBox(element.Text, element.Checked))
            {
                View.Toggled += (prev) => Prev.Toggled?.Invoke(View.Checked);
            }

            public override bool Update(CheckBoxElement element)
            {
                if (View.Text != element.Text)
                {
                    View.Text = element.Text;
                }
                if(View.Checked != element.Checked)
                {
                    View.Checked = element.Checked;
                }

                return base.Update(element);
            }
        }

        public string Text { get; set; }

        public Action<bool> Toggled { get; set; }
        public bool Checked { get; set; }

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
        public static Element CheckBox(string text = "", bool @checked = false, Action<bool> Toggled = null, Pos x = null, Pos y = null, Dim width = null, Dim height = null)
        {
            return new CheckBoxElement()
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Text = text,
                Checked = @checked,
                Toggled = Toggled,
            };
        }
    }
}

using Terminal.Gui;
using System.Linq;
using System.Collections.Generic;
using System;

namespace TodoTree.Cli
{
    public class TreeViewElement<T> : Element where T : class
    {
        class State : ElementState<TreeViewElement<T>, TreeView<T>>
        {
            public State(TreeViewElement<T> element) : base(element, new TreeView<T>())
            {

                View.AspectGetter = element.AspectGetter;
                View.TreeBuilder = element.TreeBuilder;
                View.ObjectActivated += (args) => 
                {
                    Prev.ObjectActivated?.Invoke(args.ActivatedObject);
                    View.RefreshObject(args.ActivatedObject);
                };
                View.SelectionChanged += (obj, e)=> 
                {
                    Prev.Selected?.Invoke(e.NewValue);
                };
                foreach (var value in element.Root)
                {
                    View.AddObject(value);
                }
            }


            public override bool Update(TreeViewElement<T> treeViewElement)
            {
                if (View.AspectGetter != treeViewElement.AspectGetter)
                {
                    View.AspectGetter = treeViewElement.AspectGetter;
                }

                if (View.TreeBuilder != treeViewElement.TreeBuilder)
                {
                    View.TreeBuilder = treeViewElement.TreeBuilder;
                }
                
                if(Prev.Root != treeViewElement.Root)
                {
                    var adds = treeViewElement.Root.Except(View.Objects, treeViewElement.EqualityComparer);
                    foreach (var todo in adds)
                    {
                        View.AddObject(todo);
                    }

                    var removes = View.Objects.Except(treeViewElement.Root, treeViewElement.EqualityComparer);
                    foreach(var todo in removes)
                    {
                        var remove = View.Objects.Where(obj => obj == todo).ToArray();
                        if(remove.Length > 0)
                        {
                            foreach (var item in remove)
                            {
                                View.Remove(item);
                            }
                        }
                    }
                }
                return base.Update(treeViewElement); ;
            }
        }

        public T[] Root { get; set; }
        public AspectGetterDelegate<T> AspectGetter { get; set; }
        public DelegateTreeBuilder<T> TreeBuilder { get; set; }
        public IEqualityComparer<T> EqualityComparer { get; set; }
        public Action<T> ObjectActivated { get; set; }
        public Action<T> Selected { get; set; }

        public override ElementState Create()
        {
            return new State(this);
        }

        public override bool Update(ElementState state)
        {
            return state is TreeViewElement<T>.State s && s.Update(this);
        }
    }

    public static partial class UI
    {
        public static Element TreeView<T>(T[] root, AspectGetterDelegate<T> aspectGetter, DelegateTreeBuilder<T> treeBuilder, IEqualityComparer<T> equalityComparer = null, Action<T> objectActivated = null, Action<T> selected = null, Pos x = null, Pos y = null, Dim width = null, Dim height = null) where T : class
        {
            return new TreeViewElement<T>
            {
                Root = root,
                AspectGetter = aspectGetter,
                TreeBuilder = treeBuilder,
                EqualityComparer = equalityComparer, 
                ObjectActivated = objectActivated,
                Selected = selected,
                X = x,
                Y = y,
                Width = width,
                Height = height
            };
        }
    }
}

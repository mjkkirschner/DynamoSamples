using Dynamo.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NodeUISampleViewExtension
{
    public static class WPFextensions
    {
        ///https://stackoverflow.com/questions/10279092/how-to-get-children-of-a-wpf-container-by-type
        public static T GetChildOfType<T>(this DependencyObject depObj)
    where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }
        //https://stackoverflow.com/questions/974598/find-all-controls-in-wpf-window-by-type/978352
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }

    public class NodeUISampleViewExtension : IViewExtension
    {

        private ViewLoadedParams loadedParams;

        public string Name
        {
            get
            {
                return "NodeUIInjectorViewExtension";
            }
        }

        public string UniqueId
        {
            get
            {
                return "some id";
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Loaded(ViewLoadedParams p)
        {
            this.loadedParams = p;
            p.DynamoWindow.LayoutUpdated += DynamoWindow_ContentRendered;

        }

        private void DynamoWindow_ContentRendered(object sender, EventArgs e)
        {
            var nodeViews = this.loadedParams.DynamoWindow.FindVisualChildren<NodeView>();
            foreach(var nv in nodeViews)
            {
                //if there is no existing label, add it.
                if (nv.inputGrid.Children.OfType<TextBlock>().Where(x => x.Name == "typeLabel").Count() == 0)
                {
                    var nodeTypeLabel = new TextBlock();
                    nodeTypeLabel.Name = "typeLabel";
                    nodeTypeLabel.Text = nv.ViewModel.NodeModel.CreationName;
                    nv.inputGrid.Children.Add(nodeTypeLabel);
                }
              
            }

        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public void Startup(ViewStartupParams p)
        {

        }
    }
}

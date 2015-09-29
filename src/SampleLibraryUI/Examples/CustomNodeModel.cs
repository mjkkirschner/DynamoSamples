using System;
using System.Collections.Generic;
using System.Windows;
using Autodesk.DesignScript.Runtime;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.UI.Commands;
using Dynamo.Wpf;
using ProtoCore.AST.AssociativeAST;
using SampleLibraryUI.Controls;
using SampleLibraryUI.Properties;
using SampleLibraryZeroTouch;
using Dynamo.Nodes;

namespace SampleLibraryUI.Examples
{
     /*
      * This exmple shows how to create a UI node for Dynamo
      * which loads custom data-bound UI into the node's view
      * at run time. 
     
      * Nodes with custom UI follow a different loading path
      * than zero touch nodes. The assembly which contains
      * this node needs to be located in the 'nodes' folder in
      * Dynamo in order to be loaded at startup.
     
      * Dynamo uses the MVVM model of programming, 
      * in which the UI is data-bound to the view model, which
      * exposes data from the underlying model. Custom UI nodes 
      * are a hybrid because NodeModel objects already have an
      * associated NodeViewModel which you should never need to
      * edit. So here we will create a data binding between 
      * properties on our class and our custom UI.
     */

    // The NodeName attribute is what will display on 
    // top of the node in Dynamo
    [NodeName("HydraSaveGraph")]

    // The NodeCategory attribute determines how your
    // node will be organized in the library. You can
    // specify your own category or use one of the 
    // built-ins provided in BuiltInNodeCategories.
    [NodeCategory("Hydra")]

    // The description will display in the tooltip
    // and in the help window for the node.
    [NodeDescription("A sample node to help out the Hydra team save a dynamo graph from a node")]

    // Add the IsDesignScriptCompatible attribute to ensure
    // that it gets loaded in Dynamo.
    [IsDesignScriptCompatible]
    public class HydraSaveGraph : NodeModel
    {
        private string message;
        
        public Action RequestSave;


        #region properties

      
        /// <summary>
        /// A message that will appear on the button
        /// on our node.
        /// </summary>
        public string Message
        {
            get { return message; }
            set
            {
                message = value;

                // Raise a property changed notification
                // to alert the UI that an element needs
                // an update.
                RaisePropertyChanged("NodeMessage");
            }
        }

        /// <summary>
        /// DelegateCommand objects allow you to bind
        /// UI interaction to methods on your data context.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public DelegateCommand MessageCommand { get; set; }

        #endregion

        #region constructor

        /// <summary>
        /// The constructor for a NodeModel is used to create
        /// the input and output ports and specify the argument
        /// lacing.
        /// </summary>
        public HydraSaveGraph()
        {
            
            InPortData.Add(new PortData("file path", "for now we'll just save to this filepath"));

            // This call is required to ensure that your ports are
            // properly created.
            RegisterAllPorts();

            // The arugment lacing is the way in which Dynamo handles
            // inputs of lists. If you don't want your node to
            // support argument lacing, you can set this to LacingStrategy.Disabled.
            ArgumentLacing = LacingStrategy.Disabled;

            // We create a DelegateCommand object which will be 
            // bound to our button in our custom UI. Clicking the button 
            // will call the ShowMessage method.
            MessageCommand = new DelegateCommand(ShowMessage, CanShowMessage);

            // Setting our property here will trigger a 
            // property change notification and the UI 
            // will be updated to reflect the new value.
            Message = "click to save the graph";
        }

        #endregion

        #region command methods

        private static bool CanShowMessage(object obj)
        {
           
            return true;
        }

        private void ShowMessage(object obj)
        {
          //here is where the button command will be called, we can raise another event here
            //that will raise something inside NodeViewCustomization...
           this.RequestSave();
        }

        #endregion
    }

    /// <summary>
    ///     View customizer for CustomNodeModel Node Model.
    /// </summary>
    public class CustomNodeModelNodeViewCustomization : INodeViewCustomization<HydraSaveGraph>
    {
        /// <summary>
        /// At run-time, this method is called during the node 
        /// creation. Here you can create custom UI elements and
        /// add them to the node view, but we recommend designing
        /// your UI declaratively using xaml, and binding it to
        /// properties on this node as the DataContext.
        /// </summary>
        /// <param name="model">The NodeModel representing the node's core logic.</param>
        /// <param name="nodeView">The NodeView representing the node in the graph.</param>
        public void CustomizeView(HydraSaveGraph model, NodeView nodeView)
        {


            // The view variable is a reference to the node's view.
            // In the middle of the node is a grid called the InputGrid.
            // We reccommend putting your custom UI in this grid, as it has
            // been designed for this purpose.
            
            // Create an instance of our custom UI class (defined in xaml),
            // and put it into the input grid.
            var helloDynamoControl = new HelloDynamoControl();
            nodeView.inputGrid.Children.Add(helloDynamoControl);
            
            // Set the data context for our control to be this class.
            // Properties in this class which are data bound will raise 
            // property change notifications which will update the UI.
            helloDynamoControl.DataContext = model;
            model.RequestSave += () => saveGraph(model,nodeView);
        }

        public void saveGraph(NodeModel model ,NodeView nodeView)
    {
        var graph = nodeView.ViewModel.DynamoViewModel.Model.CurrentWorkspace;

                var pathnode = model.InPorts[0].Connectors[0].Start.Owner;
                var colorsIndex = model.InPorts[0].Connectors[0].Start.Index;
                var startId = pathnode.GetAstIdentifierForOutputIndex(colorsIndex).Name;
                var pathMirror = nodeView.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(startId);
                var path =pathMirror.GetData().Data as string;


                graph.SaveAs(path, nodeView.ViewModel.DynamoViewModel.Model.EngineController.LiveRunnerRuntimeCore);

    }
        /// <summary>
        /// Here you can do any cleanup you require if you've assigned callbacks for particular 
        /// UI events on your node.
        /// </summary>
        public void Dispose() { }
    }

}

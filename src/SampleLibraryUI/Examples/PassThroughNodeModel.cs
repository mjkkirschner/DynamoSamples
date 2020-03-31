using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using SampleLibraryZeroTouch;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleLibraryUI.Examples
{
    [NodeName("PassThrough")]
    [NodeCategory("SampleLibraryUI.Examples")]
    [NodeDescription("A NodeModel node with no UI.")]
    //this is for documentation.
    [OutPortTypes("var[]..[]")]
    [InPortTypes("Geometry[]..[]")]
    [IsDesignScriptCompatible]

    public class PassThroughNodeModel : NodeModel
    {

        public PassThroughNodeModel()
        {
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("geometry", "returns input geometry")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("geometry", "input geometry")));
            RegisterAllPorts();
            ArgumentLacing = LacingStrategy.Disabled;
        }

        //constructor for deserialization
        [JsonConstructor]
        private PassThroughNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts)
                : base(inPorts, outPorts)
        {
        }

        /// <summary>
        /// BuildOutputAst is where the outpubyts of this node are calculated.
        /// This method is used to do the work that a compiler usually does 
        /// by parsing the inputs List inputAstNodes into an abstract syntax tree.
        /// </summary>
        /// <param name="inputAstNodes"></param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {

            //these will be the inputs to our function call:
            //the geometry input to the node, and a second input using the nodes guid.
            var inputs = inputAstNodes.Concat
                    (new List<AssociativeNode>() { AstFactory.BuildStringNode(this.GUID.ToString()) }).ToList();

            //WARNING:
            //an important thing to note is that our inspection pointer below returns the geometry
            //we pass in.

            //we need to make sure that the assignment to the output of the node has a dependency 
            //on this function so we know it is executed. It's possible the compiler might optimize it away
            //otherwise.

            AssociativeNode inspectGeomFunctionPointer =
                AstFactory.BuildFunctionCall(
                    new Func<IList, string, IList>(SampleUtilities.InspectGeometry), inputs);

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), inspectGeomFunctionPointer),

            };
        }
    }

    }

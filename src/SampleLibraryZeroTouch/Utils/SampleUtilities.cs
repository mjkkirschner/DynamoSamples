using System;
using System.Diagnostics;
using System.Linq;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;

namespace SampleLibraryZeroTouch
{
    /// <summary>
    /// A utility library containing methods that can be called 
    /// from NodeModel nodes, or used as nodes in Dynamo.
    /// </summary>
    public static class SampleUtilities
    {
        [IsVisibleInDynamoLibrary(false)]
        public static double MultiplyInputByNumber(double input)
        {
            return input * 10;
        }

        [IsVisibleInDynamoLibrary(false)]
        public static string DescribeButtonMessage(string input)
        {
            return "Button displays: " + input;
        }

        [IsVisibleInDynamoLibrary(false)]
        public static string DescribeWindowMessage(string GUID, string input)
        {
            return "Window displays: Data bridge callback of node " + GUID.Substring(0, 5) + ": " + input;
        }

        [IsVisibleInDynamoLibrary(false)]
        public static Geometry[] GenerateSomeGeom(string path)
        {
            //return Autodesk.DesignScript.Geometry.Geometry.ImportFromSAT(path);

            //For now just going to geneate some spheres. Let's pretend they came from sat import above.
            //I would also usually call dispose on the points, since we dont return them - though this should now be safe in dynamo 2.5
            //use random numbers so we can tell when the node is re-executed.
            var randomNum = new Random().Next(1, 30);
            var randomRadius = new Random().NextDouble() * 3.0;
            return Enumerable.Range(0, randomNum).Select(x => Point.ByCoordinates(x, x, x)).Select(pt => Sphere.ByCenterPointRadius(pt, randomRadius)).ToArray();

        }

        public static Geometry[] InspectGeometry(Geometry[] geometries, string guid)
        {
            Debug.WriteLine($"there were {geometries.Count()} geomery objects passed to this function");
            Debug.WriteLine($"this node has id {guid}");
            return geometries;
        }
    }
}

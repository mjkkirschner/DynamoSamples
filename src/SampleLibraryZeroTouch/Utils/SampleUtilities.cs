using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using System.Linq;

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

        
        public static IList InspectGeometry(IList geometries, string guid)
        {
            var flattened = Flatten(geometries).Cast<object>().ToList();
;            Debug.WriteLine($"there were {flattened.Count()} geomery objects passed to this function");
            Debug.WriteLine($"this node has id {guid}");
            return geometries;
        }


        //BELOW CODE IS LIFTED FROM DYNAMO'S CORE LIST FLATTEN IMPLEMENTATION
        //MAYBE DONT USE IT.

        public static IList Flatten(IList list, int amt = -1)
        {
            if (amt < 0)
            {
                return Flatten(list, GetDepth(list), new List<object>());
            }
            return Flatten(list, amt, new List<object>());
        }

        private static int GetDepth(object list)
        {
            if (!(list is IList)) return 0;

            int depth = 1;
            foreach (var obj in (IList)list) // If it is a list, check if it contains a sublist
            {
                if (obj is IList) // If it contains a sublist
                {
                    int d = 1 + GetDepth((IList)obj);
                    depth = (depth > d) ? depth : d; // Get the maximum depth among all items
                }
            }
            return depth;
        }

        private static IList Flatten(IList list, int amt, IList acc)
        {
            if (amt == 0)
            {
                foreach (object item in list)
                    acc.Add(item);
            }
            else
            {
                foreach (object item in list)
                {
                    if (item is IList)
                        acc = Flatten(item as IList, amt - 1, acc);
                    else
                        acc.Add(item);
                }
            }
            return acc;
        }

    }
}

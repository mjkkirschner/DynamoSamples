using Autodesk.DesignScript.Runtime;
using System.Collections.Generic;

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
        public static List<object> GetObjects(string s)
        {
            List<object> r = new List<object>();
            
            r.Add(new Dictionary<string, string>() { { "1", "A" }, { "2", "B" }, { "3", "C" } });

            return r;
        }
    }
}

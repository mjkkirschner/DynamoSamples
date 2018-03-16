using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf.ViewModels.Watch3D;
using System.IO;
using System.Windows.Threading;
using static Dynamo.ViewModels.DynamoViewModel;
using System.Collections;
using DesignScript.Builtin;
using Newtonsoft.Json;
using System.Threading;
using Dynamo.Scheduler;
using Dynamo.Interfaces;

namespace Dynamo.Meta
{
    public class DynamoMeta
    {
        private DynamoMeta()
        {

        }

        private class DynamoEnginePathResolver : IPathResolver
        {
            private readonly List<string> additionalResolutionPaths;
            private readonly List<string> additionalNodeDirectories;
            private readonly List<string> preloadedLibraryPaths;

            public DynamoEnginePathResolver(string preloaderLocation)
            {
    
                additionalResolutionPaths = new List<string>();
                if (Directory.Exists(preloaderLocation))
                    additionalResolutionPaths.Add(preloaderLocation);

                additionalNodeDirectories = new List<string>();
                preloadedLibraryPaths = new List<string>
            {
                "VMDataBridge.dll",
                "ProtoGeometry.dll",
                "DesignScriptBuiltin.dll",
                "DSCoreNodes.dll",
                "DSOffice.dll",
                "DSIronPython.dll",
                "FunctionObject.ds",
                "BuiltIn.ds",
                "DynamoConversions.dll",
                "DynamoUnits.dll",
                "Tessellation.dll",
                "Analysis.dll",
                "GeometryColor.dll"
            };

            }

            public IEnumerable<string> AdditionalResolutionPaths
            {
                get { return additionalResolutionPaths; }
            }

            public IEnumerable<string> AdditionalNodeDirectories
            {
                get { return additionalNodeDirectories; }
            }

            public IEnumerable<string> PreloadedLibraryPaths
            {
                get { return preloadedLibraryPaths; }
            }

            public string UserDataRootFolder
            {
                get { return string.Empty; }
            }

            public string CommonDataRootFolder
            {
                get { return string.Empty; }
            }
        }


        private static DynamoModel makeSyncModel()
        {
            var geometryFactoryPath = string.Empty;
            var preloaderLocation = string.Empty;
            Dynamo.Applications.StartupUtils.PreloadShapeManager(ref geometryFactoryPath, ref preloaderLocation);

            //start headless mode so updaters and analytics do not start
            //start Synchronous so tasks are processed on main thread.
            var config = new DynamoModel.DefaultStartConfiguration()
            {
                GeometryFactoryPath = geometryFactoryPath,
                ProcessMode = TaskProcessMode.Synchronous,
                IsHeadless = true
            };

            config.UpdateManager = null;
            config.StartInTestMode = true;
            config.PathResolver = new DynamoEnginePathResolver(preloaderLocation);

            var model = DynamoModel.Start(config);
            return model;
        }

        private static string GetNewFileNameOnTempPath(string fileExtension = "dyn")
        {
            var guid = Guid.NewGuid().ToString();
            return Path.Combine(
                Path.GetTempPath(),
                string.IsNullOrWhiteSpace(fileExtension)
                    ? guid
                    : Path.ChangeExtension(guid, fileExtension));
        }
        public static WorkspaceModel openWorkspace(string path)
        {

            var model = makeSyncModel();

            var viewModel = DynamoViewModel.Start(
                new DynamoViewModel.StartConfiguration()
                {
                    DynamoModel = model
                });
            viewModel.OpenCommand.Execute(path);
            //TODO seems we need to do this or later runs will be missing their engine?..somehow attaching to this.
            //viewModel.PerformShutdownSequence(new ShutdownParams(false, false));
            var output = model.CurrentWorkspace;
            return output;

        }

        public static string convertToJson(WorkspaceModel ws)
        {
            var model = makeSyncModel();

            var viewModel = DynamoViewModel.Start(
                new DynamoViewModel.StartConfiguration()
                {
                    DynamoModel = model
                });
            //TODO gather any notifications or messages this command logs? Like unresolved nodes or other conversion failures.

            model.AddWorkspace(ws);
            model.CurrentWorkspace = ws;
            viewModel.CurrentWorkspaceIndex = 0;
            var savePath = GetNewFileNameOnTempPath();
            viewModel.SaveAs(savePath);
            viewModel.PerformShutdownSequence(new ShutdownParams(false, false));
            var output = System.IO.File.ReadAllText(savePath);
            return output;
        }
        private static string GetStringRepOfCollection(ProtoCore.Mirror.MirrorData value)
        {
            var items = string.Join(",",
                value.GetElements().Select(x =>
                {
                    if (x.IsCollection) return GetStringRepOfCollection(x);
                    return x.IsDictionary ? GetStringRepOfDictionary(x.Data) : x.StringData;
                }));
            return "{" + items + "}";
        }

        private static string GetStringRepOfDictionary(object value)
        {
            if (value is DesignScript.Builtin.Dictionary || value is IDictionary)
            {
                IEnumerable<string> keys;
                IEnumerable<object> values;
                var dictionary = value as Dictionary;
                if (dictionary != null)
                {
                    var dict = dictionary;
                    keys = dict.Keys;
                    values = dict.Values;
                }
                else
                {
                    var dict = (IDictionary)value;
                    keys = dict.Keys.Cast<string>();
                    values = dict.Values.Cast<object>();
                }
                var items = string.Join(", ", keys.Zip(values, (str, obj) => str + " : " + GetStringRepOfDictionary(obj)));
                return "{" + items + "}";
            }
            if (!(value is string) && value is IEnumerable)
            {
                var list = ((IEnumerable)value).Cast<dynamic>().ToList();
                var items = string.Join(", ", list.Select(x => GetStringRepOfDictionary(x)));
                return "{" + items + "}";
            }
            return value.ToString();
        }

        private static Dictionary<Guid, List<string>> gatherResultsFromWorkspace(DynamoModel model, WorkspaceModel ws)
        {
            var resultsdict = new Dictionary<Guid, List<string>>();
            foreach (var node in ws.Nodes)
            {
                var portvalues = new List<string>();
                foreach (var port in node.OutPorts)
                {
                    var value = node.GetValue(port.Index, model.EngineController);
                    if (value.IsCollection)
                    {
                        portvalues.Add(GetStringRepOfCollection(value));
                    }
                    else if (value.IsDictionary)
                    {
                        portvalues.Add(GetStringRepOfDictionary(value.Data));
                    }
                    else
                    {
                        portvalues.Add(value.StringData);
                    }
                }

                resultsdict.Add(node.GUID, portvalues);
            }
            return resultsdict;
        }

        public static string runWorkspace(WorkspaceModel ws)
        {

            var model = makeSyncModel();
            model.AddWorkspace(ws);
            model.CurrentWorkspace = ws;
            model.ForceRun();

            //TODO attempt to return the results directly...
            var results = gatherResultsFromWorkspace(model, model.CurrentWorkspace);

            return JsonConvert.SerializeObject(results);
        }
    }
}

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
using System.Reflection;
using System.Windows;
using ProtoCore.Mirror;
using Dynamo.Graph;
using System.Xml;
using Dynamo.Migration;
using Dynamo.Controls;
using System.Windows.Media;

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
                IsHeadless = true,
                //TODO to stop backup timer, maybe use test mode or just set time to 0;
                //before the model is created.
                StartInTestMode = true,
               
            };

            config.UpdateManager = null;
            config.StartInTestMode = true;
            config.PathResolver = new DynamoEnginePathResolver(preloaderLocation);
            config.Context = "DynamoMeta";
            

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
        public static WorkspaceModel openWorkspace(DynamoViewModel vm,string path)
        {
            vm.OpenCommand.Execute(path);
            var output = vm.Model.CurrentWorkspace;
            return output;
        }

        public static DynamoViewModel createDynamoEngine()
        {
            var model = makeSyncModel();

            var viewModel = DynamoViewModel.Start(
                new DynamoViewModel.StartConfiguration()
                {
                    DynamoModel = model
                });
            //set the dispatcher of this viewModel to the current UI dispatcher so we don't crash.
            var dispatcherSetter = typeof(DynamoViewModel).
                GetProperty("UIDispatcher",BindingFlags.Instance | BindingFlags.NonPublic).SetMethod;
            dispatcherSetter.Invoke(viewModel, new object[] { Application.Current.Dispatcher });

            return viewModel;
        }


        public static DynamoViewModel getCurrenDynamoEngine()
        {
            return Application.Current.Dispatcher.Invoke(new Func<DynamoViewModel>(() =>
            {
                var results = Application.Current.Windows.Cast<Window>().OfType<DynamoView>();
                var window = results.FirstOrDefault();
                if (window != null)
                {
                    return window.DataContext as DynamoViewModel;
                }
                return null;
            }));  
        }

        public static string saveToJson(DynamoViewModel dynamoEngine, string path)
        {
            dynamoEngine.OpenCommand.Execute(path);
            var savePath = GetNewFileNameOnTempPath();
            dynamoEngine.SaveAs(savePath);
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

        private static List<object> gatherMirrorResults(DynamoModel model,WorkspaceModel ws)
        {
            var resultsdict = new Dictionary<string, object>();
             foreach (var node in ws.Nodes)
            {
                var portvalues = new List<object>();
                foreach (var port in node.OutPorts)
                {
                    var value = node.GetValue(port.Index, model.EngineController);
                    if (value.IsCollection)
                    {
                        portvalues.Add(getMirrorDataFromCollection(value));
                    }
                    else if (value.IsDictionary)
                    {
                        portvalues.Add(getMirrorDataFromDictionary(value.Data));
                    }
                    else
                    {
                        portvalues.Add(value.Data);
                    }
                }

                resultsdict.Add(node.GUID.ToString(), portvalues);
            }
            return resultsdict.Values.ToList();
        }

        private static IEnumerable<object> getMirrorDataFromCollection(MirrorData value)
        {

            return value.GetElements().Select(x =>
            {
                if (x.IsCollection) return getMirrorDataFromCollection(x);
                return x.IsDictionary ? getMirrorDataFromDictionary(x.Data) : x.Data;
            });
          
        }

        private static IDictionary<string,object> getMirrorDataFromDictionary(object value)
        {
            return (value as Dictionary).Components();
        }

        public static string runWorkspace(DynamoViewModel dynamoEngine, WorkspaceModel ws)
        {
            var model = dynamoEngine.Model;
            model.AddWorkspace(ws);
            model.CurrentWorkspace = ws;
            model.ForceRun();
            var results = gatherResultsFromWorkspace(model, model.CurrentWorkspace);

            return JsonConvert.SerializeObject(results);
        }
        public static object runWorkspaceDangerous(DynamoViewModel dynamoEngine,WorkspaceModel ws)
        {
            var model = dynamoEngine.Model;
            model.AddWorkspace(ws);
            model.CurrentWorkspace = ws;
            model.ForceRun();
            //TODO this will only work if the nodes in the graph return lists of rank1.
            return gatherMirrorResults(model, model.CurrentWorkspace);

        }
    }
}

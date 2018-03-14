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

namespace Dynamo.Meta
{
    public class DynamoMeta
    {
        private DynamoMeta()
        {

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
            Func<string, WorkspaceModel> function = (string filePath) =>
              {
                //TODO don't use CLI config - we'll need our own config
                //probably with paths of dlls to load which are required for the graph.
                var model = Dynamo.Applications.StartupUtils.MakeModel(true);

                  var viewModel = DynamoViewModel.Start(
                      new DynamoViewModel.StartConfiguration()
                      {
                          DynamoModel = model
                      });
                //TODO gather any notifications or messages this command logs?

                viewModel.OpenCommand.Execute(path);
                  viewModel.PerformShutdownSequence(new ShutdownParams(false,false));
                  return model.CurrentWorkspace;
              };
            return Dispatcher.CurrentDispatcher.Invoke(function,path) as WorkspaceModel;
        }

        public static string convertToJson(WorkspaceModel ws)
        {
            Func<WorkspaceModel,string> function = (WorkspaceModel ws2) =>
            {
                //TODO don't use CLI config - we'll need our own config
                //probably with paths of dlls to load which are required for the graph.
                var model = Dynamo.Applications.StartupUtils.MakeModel(true);

                var viewModel = DynamoViewModel.Start(
                    new DynamoViewModel.StartConfiguration()
                    {
                        DynamoModel = model
                    });
                //TODO gather any notifications or messages this command logs?

                model.OpenFileFromPath(ws.FileName);
                var savePath = GetNewFileNameOnTempPath();
                viewModel.SaveAs(savePath);
                viewModel.PerformShutdownSequence(new ShutdownParams(false, false));
                return System.IO.File.ReadAllText(savePath);

            };

           return Dispatcher.CurrentDispatcher.Invoke(function, ws) as string;
           
        }
    }
}

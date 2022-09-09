using AsyncHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples
{
    internal class AsyncExamplesUsingTasks
    {
        public async Task Run()
        {
            await ExecuteAllTasksAtOnce();
            Console.WriteLine(string.Empty);

            await ExecuteAllTasksAtOnceWithParameters();
            Console.WriteLine(string.Empty);
        }

        public async Task ExecuteAllTasksAtOnce()
        {
            var files = new MockRemoteFile[5]
            {
                new MockRemoteFile(){ Seconds = 5 },
                new MockRemoteFile(){ Seconds = 4 },
                new MockRemoteFile(){ Seconds = 3 },
                new MockRemoteFile(){ Seconds = 2 },
                new MockRemoteFile(){ Seconds = 1}
            };
            var tasks = (from file in files select DownloadFile(file)).ToArray();

            var asyncHelper = new AsyncTaskHelper();

            int i = 0;
            await foreach (var file in asyncHelper.GetTasksAsTheyComplete(tasks))
            {
                i++;
                Console.WriteLine($"Task #{i} completed: {file.Result}");                
            }
        }

        public async Task ExecuteAllTasksAtOnceWithParameters()
        {
            var files = new MockRemoteFile[5]
            {
                new MockRemoteFile(){ Seconds = 5 },
                new MockRemoteFile(){ Seconds = 4 },
                new MockRemoteFile(){ Seconds = 3 },
                new MockRemoteFile(){ Seconds = 2 },
                new MockRemoteFile(){ Seconds = 1}
            };
            var tasks = (from file in files select DownloadFile(file)).ToArray();

            var asyncHelper = new AsyncTaskHelper();

            int i = 0;
            await foreach (var taskResult in asyncHelper.GetTasksAsTheyComplete(tasks, CancellationToken.None, int.MaxValue, LogDownloadStart, DeleteFile))
            {
                i++;
                Console.WriteLine($"Task #{i} completed: {taskResult.Result}");                
            }
        }

        private async Task<string> DownloadFile(MockRemoteFile file)
        {
            await file.Download();
            return $"Dowloaded the file that takes {file.Seconds}s to download.";
        }

        private Task LogDownloadStart()
        {            
            Console.WriteLine($"Starting to download a file.");
            return Task.CompletedTask;
        }

        private Task DeleteFile(string taskResult)
        {            
            Console.WriteLine($"Deleted the file with result: {taskResult}");
            return Task.CompletedTask;
        }
    }
}

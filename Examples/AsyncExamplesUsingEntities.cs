using AsyncHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples
{
    internal class AsyncExamplesUsingEntities
    {
        public async Task Run()
        {
            await DownloadAllFilesAtOnce();
            Console.WriteLine(string.Empty);

            await DownloadAllFilesAtOnceWithParameters();
            Console.WriteLine(string.Empty);
        }

        public async Task DownloadAllFilesAtOnce()
        {
            var files = new MockRemoteFile[5] 
            {
                new MockRemoteFile(){ Seconds = 5 },
                new MockRemoteFile(){ Seconds = 4 },
                new MockRemoteFile(){ Seconds = 3 },
                new MockRemoteFile(){ Seconds = 2 },
                new MockRemoteFile(){ Seconds = 1}
            };
            var asyncHelper = new AsyncTaskHelper();
            int i = 0;
            await foreach(var file in asyncHelper.GetTasksAsTheyComplete(files, DownloadFile))
            {
                i++;
                Console.WriteLine($"Task #{i} completed: {file.Result}");
            }
        }

        public async Task DownloadAllFilesAtOnceWithParameters()
        {
            var files = new MockRemoteFile[5]
            {
                new MockRemoteFile(){ Seconds = 5 },
                new MockRemoteFile(){ Seconds = 4 },
                new MockRemoteFile(){ Seconds = 3 },
                new MockRemoteFile(){ Seconds = 2 },
                new MockRemoteFile(){ Seconds = 1}
            };
            var asyncHelper = new AsyncTaskHelper();
            int i = 0;
            await foreach (var taskResult in asyncHelper.GetTasksAsTheyComplete(files, DownloadFile, CancellationToken.None, int.MaxValue, LogDownloadStart, DeleteFile))
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

        private Task LogDownloadStart(MockRemoteFile file)
        {            
            Console.WriteLine($"Starting to download the file that takes {file.Seconds}s to download.");
            return Task.CompletedTask;
        }

        private async Task DeleteFile(MockRemoteFile file, string taskResult)
        {
            await file.Delete();
            Console.WriteLine($"Deleted the file that takes {file.Seconds}s to download.");
        }
    }
}

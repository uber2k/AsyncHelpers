using AsyncHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples
{
    internal class AsyncExamples
    {
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

        private async Task<string> DownloadFile(MockRemoteFile file)
        {
            await file.Download();
            return $"Dowloaded the file that takes {file.Seconds}s to download.";
        }

        private async Task<string> DeleteFile(MockRemoteFile file)
        {
            await file.Delete();
            return $"Deleted the file that takes {file.Seconds}s to download.";
        }
    }
}

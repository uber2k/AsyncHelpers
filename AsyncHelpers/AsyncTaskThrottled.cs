using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AsyncHelpers
{
    public class AsyncTaskHelper
    {
        public async IAsyncEnumerable<ThrottledTaskResult<T>> GetTasksAsTheyComplete<T, E>(E[] entities, Func<E, Task<T>> taskFunction,
             int simultaneousTasks = int.MaxValue, [EnumeratorCancellation] CancellationToken token = default, 
             Func<T, Task>? onTaskCompleted = null, Func<Task<T>, Task>? onTaskException = null)
        {
            var tasks = new Task<T>[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                int index = i;
                tasks[index] = Task.Run<T>(() => {
                    return taskFunction(entities[index]);
                });
            }

            await foreach (var completedTask in GetTasksAsTheyComplete<T>(tasks, simultaneousTasks, token, onTaskCompleted, onTaskException))
            {
                yield return completedTask;
            }
        }

        public async IAsyncEnumerable<ThrottledTaskResult<T>> GetTasksAsTheyComplete<T>(Task<T>[] tasks, int simultaneousTasks = int.MaxValue,
           [EnumeratorCancellation] CancellationToken token = default, Func<T, Task>? onTaskCompleted = null, Func<Task<T>, Task>? onTaskException = null)
        {
            var semaphore = new SemaphoreSlim(simultaneousTasks);            
            using (semaphore)
            {
                var resultTasks = new Task<ThrottledTaskResult<T>>[tasks.Length];
                for (int i = 0; i < tasks.Length; i++)
                {
                    int index = i;
                    resultTasks[i] = Task.Run<ThrottledTaskResult<T>>(() => {
                        return ExecuteTaskThrottled(tasks[index], semaphore, token, onTaskCompleted, onTaskException);
                    });
                }

                await foreach (var completedTask in resultTasks.GetTasksAsTheyCompleteAsync(token))
                {
                    yield return await completedTask;
                }
            }
        }

        private async Task<ThrottledTaskResult<T>> ExecuteTaskThrottled<T>(Task<T> task, SemaphoreSlim semaphore,
                  CancellationToken token = default, Func<T, Task>? onTaskCompleted = null, Func<Task<T>, Task>? onTaskException = null)
        {
            token.ThrowIfCancellationRequested();
            var result = new ThrottledTaskResult<T>();
            
            await semaphore.WaitAsync(token);
            try
            {
                 result.TaskStarted = DateTime.Now;
                 var taskResult = await task;
                 result.Result = taskResult;
                 result.TaskEnded = DateTime.Now;

                if (onTaskCompleted != null)
                {
                    await onTaskCompleted(taskResult);
                }
            }         
            catch (Exception e)
            {             
                result.HadErrors = true;
                result.Exception = e;
                if (onTaskException != null)
                {
                    await onTaskException(task);
                }
            }
            finally
            {
                semaphore.Release();
            }
            return result;            
        }

      
    }
}

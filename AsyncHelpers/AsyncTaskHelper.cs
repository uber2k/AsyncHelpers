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
        public IAsyncEnumerable<AsyncHelperResult<R, E>> GetTasksAsTheyComplete<R, E>(E[] entities, Func<E, Task<R>> taskFunction,
             CancellationToken token = default, int simultaneousTasks = int.MaxValue,
             Func<E, Task>? onBeforeTaskStarted = null, Func<E, R, Task>? onTaskCompleted = null, Func<Task<R>, Task>? onTaskException = null) where E : class
        {
            var tasks = new Task<R>[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                int index = i;
                tasks[index] = Task.Run(() => {
                    return taskFunction(entities[index]);
                });
            }

            return GetTasksAsTheyCompleteWithEntities(tasks, entities, token, simultaneousTasks, onBeforeTaskStarted, onTaskCompleted, onTaskException);
        }

        public IAsyncEnumerable<AsyncHelperResult<R>> GetTasksAsTheyComplete<R>(Task<R>[] tasks, CancellationToken token = default,
           int simultaneousTasks = int.MaxValue,
            Func<Task>? onBeforeTaskStarted = null, 
            Func<R, Task>? onTaskCompleted = null,
            Func<Task<R>, Task>? onTaskException = null)
        {
            return GetTasksAsTheyCompleteNoEntities(tasks, token, simultaneousTasks, onBeforeTaskStarted, onTaskCompleted, onTaskException);
        }

        private async IAsyncEnumerable<AsyncHelperResult<R, E>> GetTasksAsTheyCompleteWithEntities<R, E>(Task<R>[] tasks, E[] entities,
            [EnumeratorCancellation] CancellationToken token = default, int simultaneousTasks = int.MaxValue,
            Func<E, Task>? onBeforeTaskStarted = null, 
            Func<E, R, Task>? onTaskCompleted = null,
            Func<Task<R>, Task>? onTaskException = null) where E : class
        {
            var semaphore = new SemaphoreSlim(simultaneousTasks);
            using (semaphore)
            {
                var resultTasks = new Task<AsyncHelperResult<R, E>>[tasks.Length];
                for (int i = 0; i < tasks.Length; i++)
                {
                    int index = i;                 
                    var entity = entities[index];
                    resultTasks[i] = Task.Run(() =>
                    {
                        return ExecuteTaskThrottled(tasks[index], entity, semaphore, token, onBeforeTaskStarted, onTaskCompleted, onTaskException);
                    });                                     
                }

                await foreach (var completedTask in resultTasks.GetTasksAsTheyCompleteAsync(token))
                {
                    yield return await completedTask;
                }
            }
        }

        private async IAsyncEnumerable<AsyncHelperResult<R>> GetTasksAsTheyCompleteNoEntities<R>(Task<R>[] tasks,
           [EnumeratorCancellation] CancellationToken token = default, int simultaneousTasks = int.MaxValue,
          Func<Task>? onBeforeTaskStartedNoEntity = null,
          Func<R, Task>? onTaskCompletedNoEntity = null,
          Func<Task<R>, Task>? onTaskException = null) 
        {
            var semaphore = new SemaphoreSlim(simultaneousTasks);
            using (semaphore)
            {
                var resultTasks = new Task<AsyncHelperResult<R>>[tasks.Length];
                for (int i = 0; i < tasks.Length; i++)
                {
                    int index = i;    
                    resultTasks[i] = Task.Run(() =>
                    {
                        return ExecuteTaskThrottledNoEntity(tasks[index], semaphore, token, onBeforeTaskStartedNoEntity, onTaskCompletedNoEntity, onTaskException);
                    });                    
                }

                await foreach (var completedTask in resultTasks.GetTasksAsTheyCompleteAsync(token))
                {
                    yield return await completedTask;
                }
            }
        }   

        private async Task<AsyncHelperResult<R, E>> ExecuteTaskThrottled<E, R>(Task<R> task, E? entity, SemaphoreSlim semaphore,
                CancellationToken token = default, Func<E, Task>? onBeforeTaskStarted = null,
                Func<E, R, Task>? onTaskCompleted = null, Func<Task<R>, Task>? onTaskException = null) where E : class
        {
            token.ThrowIfCancellationRequested();
            var result = new AsyncHelperResult<R, E>();

            await semaphore.WaitAsync(token);
            try
            {
                await ExecuteTask(task, entity, result, onBeforeTaskStarted, null, onTaskCompleted, null);
                result.Entity = entity;
            }
            catch (Exception e)
            {
                await OnExecuteException(task, onTaskException, result, e);
            }
            finally
            {
                semaphore.Release();
            }
            return result;
        }       

        private async Task<AsyncHelperResult<R>> ExecuteTaskThrottledNoEntity<R>(Task<R> task, SemaphoreSlim semaphore,
                  CancellationToken token = default, 
                  Func<Task>? onBeforeTaskStartedNoEntity = null, 
                  Func<R, Task>? onTaskCompletedNoEntity = null,
                  Func<Task<R>, Task>? onTaskException = null)
        {
            token.ThrowIfCancellationRequested();
            var result = new AsyncHelperResult<R>();
            
            await semaphore.WaitAsync(token);
            try
            {
                await ExecuteTask<string, R>(task, null, result, null, onBeforeTaskStartedNoEntity, null, onTaskCompletedNoEntity);
            }         
            catch (Exception e)
            {
                await OnExecuteException(task, onTaskException, result, e);
            }
            finally
            {
                semaphore.Release();
            }
            return result;            
        }

        private static async Task ExecuteTask<E, R>(Task<R> task, E? entity, AsyncHelperResult<R> result, Func<E, Task>? onBeforeTaskStarted,
            Func<Task>? onBeforeTaskStartedNoEntity, Func<E, R, Task>? onTaskCompleted, Func<R, Task>? onTaskCompletedNoEntity = null) where E : class
        {
            if (onBeforeTaskStarted != null && entity != null)
            {
                await onBeforeTaskStarted(entity);
            }

            if (onBeforeTaskStartedNoEntity != null)
            {
                await onBeforeTaskStartedNoEntity();
            }

            result.TaskStarted = DateTime.Now;
            var taskResult = await task;
            result.Result = taskResult;
            result.TaskEnded = DateTime.Now;

            if (onTaskCompleted != null && entity != null)
            {
                await onTaskCompleted(entity, taskResult);
            }

            if (onTaskCompletedNoEntity != null)
            {
                await onTaskCompletedNoEntity(taskResult);
            }
        }

        private async Task OnExecuteException<R>(Task<R> task, Func<Task<R>, Task>? onTaskException, AsyncHelperResult<R> result, Exception e)
        {
            result.HadErrors = true;
            result.Exception = e;
            if (onTaskException != null)
            {
                await onTaskException(task);
            }
        }
    }
}

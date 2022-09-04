using System.Runtime.CompilerServices;

namespace AsyncHelpers
{
    public static class AsyncTaskResultExtensions
    {
        /// <summary>
        /// Stores the results for the specified tasks in each element of the array, in the order that they complete.
        /// When consumed by awaiting in a foreach, has the practical result of retrieving tasks as the complete.
        /// Alternatively, awaiting by index would wait for and retrieve the task that completed in that position (await GetTaskResults()[3] waits for and returns the 4th task to complete).
        /// </summary>
        /// <typeparam name="T">Type of the tasks</typeparam>
        /// <param name="tasks">Tasks to be run</param>
        /// <returns>Array of tasks, that contain the original tasks in the order that they completed</returns>
        public static Task<Task<T>>[] GetTasksAsTheyComplete<T>(this Task<T>[] tasks,
            CancellationToken cancellationToken = default)
        {
            var inputTasks = tasks;

            var resultBuckets = new TaskCompletionSource<Task<T>>[tasks.Length];
            var results = new Task<Task<T>>[resultBuckets.Length];
            for (int i = 0; i < resultBuckets.Length; i++)
            {
                resultBuckets[i] = new TaskCompletionSource<Task<T>>();
                results[i] = resultBuckets[i].Task;
            }

            //action that will run for each task after it completes
            //stores the task in the next available result bucket
            int nextTaskIndex = -1;
            Action<Task<T>> afterTaskCompletionTask = (completedTask) =>
            {
                var bucket = resultBuckets[Interlocked.Increment(ref nextTaskIndex)];
                bucket.TrySetResult(completedTask);
            };

            //set the continuation action for each received task
            foreach (var inputTask in inputTasks)
            {
                inputTask.ContinueWith(afterTaskCompletionTask, cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            return results;
        }

        /// <summary>
        /// Returns the passed tasks as they complete
        /// </summary>
        /// <typeparam name="T">Type of tasks</typeparam>
        /// <param name="tasks">Tasks</param>
        /// <param name="cancellationToken">Cacnellation token to halt enumeration</param>
        /// <returns>Tasks sent as parameter, in the order that they completed</returns>
        public static async IAsyncEnumerable<Task<T>> GetTasksAsTheyCompleteAsync<T>(
            this Task<T>[] tasks, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var resultBucket in tasks.GetTasksAsTheyComplete(cancellationToken))
            {
                var completedTask = await resultBucket;
                yield return completedTask;
            }
        }
       
    }
}
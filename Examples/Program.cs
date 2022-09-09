// See https://aka.ms/new-console-template for more information
using Examples;

await new AsyncExamplesUsingEntities().DownloadAllFilesAtOnce();
Console.WriteLine(string.Empty);

await new AsyncExamplesUsingEntities().DownloadAllFilesAtOnceWithParameters();
Console.WriteLine(string.Empty);

await new AsyncExamplesUsingTasks().ExecuteAllTasksAtOnce();
Console.WriteLine(string.Empty);

await new AsyncExamplesUsingTasks().ExecuteAllTasksAtOnceWithParameters();
Console.WriteLine(string.Empty);

Console.ReadLine();
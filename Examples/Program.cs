// See https://aka.ms/new-console-template for more information
using Examples;

await new AsyncExamplesUsingEntities().Run();
Console.WriteLine(string.Empty);

await new AsyncExamplesUsingTasks().Run();
Console.WriteLine(string.Empty);

Console.ReadLine();
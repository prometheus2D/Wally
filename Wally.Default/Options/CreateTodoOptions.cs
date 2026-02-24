using CommandLine;

namespace Wally.Default.Options
{
    [Verb("create-todo", HelpText = "Create a Todo app at the specified path.")]
    public class CreateTodoOptions
    {
        [Value(0, Required = true, HelpText = "The path to create the Todo app.")]
        public string Path { get; set; }
    }
}
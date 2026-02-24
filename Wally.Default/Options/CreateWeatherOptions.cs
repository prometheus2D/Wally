using CommandLine;

namespace Wally.Default.Options
{
    [Verb("create-weather", HelpText = "Create a Weather app at the specified path.")]
    public class CreateWeatherOptions
    {
        [Value(0, Required = true, HelpText = "The path to create the Weather app.")]
        public string Path { get; set; }
    }
}
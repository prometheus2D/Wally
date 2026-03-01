namespace Wally.Core.Docs
{
    /// <summary>
    /// A single documentation file loaded from disk.
    /// Injected into actor prompts as reference context.
    /// </summary>
    public sealed class Document
    {
        /// <summary>The file name (e.g. <c>character-guide.md</c>).</summary>
        public string Name { get; }

        /// <summary>The full text content of the file.</summary>
        public string Content { get; }

        public Document(string name, string content)
        {
            Name = name;
            Content = content;
        }

        public override string ToString() => $"[{Name}] ({Content.Length} chars)";
    }
}

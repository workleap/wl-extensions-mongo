using System.Text.RegularExpressions;

namespace Workleap.Extensions.Mongo;

[AttributeUsage(AttributeTargets.Class)]
public sealed class MongoCollectionAttribute : Attribute
{
    private static readonly Regex ValidNameRegex = new Regex("^[a-zA-Z][a-zA-Z0-9_]{0,63}$", RegexOptions.Compiled);

    public MongoCollectionAttribute(string name)
    {
        this.Name = ValidNameRegex.IsMatch(name) ? name : throw new ArgumentException("Collection name must match the regex " + ValidNameRegex, nameof(name));
    }

    public string Name { get; }

    public string? DatabaseName { get; set; }

    public Type? IndexProviderType { get; set; }
}
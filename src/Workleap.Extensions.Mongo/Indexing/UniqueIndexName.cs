using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;

namespace Workleap.Extensions.Mongo.Indexing;

/// <summary>
/// Represents a unique index name generated from an instance of CreateIndexModel&lt;TDocument&gt;.
/// The index name is composed of the developer-entered index name concatenated with a partial 32 char SHA256 hash of the index definition.
/// We considered that using the half of the SHA256 hash is actually good enough to prevent collisions with other indexes for a given document.
/// Any change to the index definition (new fields, uniqueness, etc.) will change the hash, and that's how we can know if an index should be updated.
/// </summary>
internal sealed class UniqueIndexName
{
    private const int IndexPartialHashLength = 32;

    // Careful, pre-4.2 MongoDB has a index name length limit of 127 characters
    // https://www.mongodb.com/docs/manual/reference/limits/#mongodb-limit-Index-Name-Length
    private static readonly Regex ValidNameRegex = new Regex(
        "^(?<Prefix>[a-zA-Z0-9_]+)_(?<Hash>[a-z0-9]{" + IndexPartialHashLength + "})$",
        RegexOptions.Compiled);

    private UniqueIndexName()
    {
    }

    public string FullName { get; private set; } = string.Empty;

    public string Prefix { get; private set; } = string.Empty;

    public string Hash { get; private set; } = string.Empty;

    public static bool TryCreate<TDocument>(CreateIndexModel<TDocument> indexModel, [MaybeNullWhen(false)] out UniqueIndexName indexName)
    {
        var bsonRenderArgs = new RenderArgs<TDocument>(BsonSerializer.LookupSerializer<TDocument>(), BsonSerializer.SerializerRegistry);

        var options = indexModel.Options;
        var prefix = options?.Name?.Trim() ?? IndexNameHelper.GetIndexName(indexModel.Keys.Render(bsonRenderArgs));
        if (prefix.Length == 0)
        {
            indexName = null;
            return false;
        }

        var bsonIndexFields = indexModel.Keys.Render(bsonRenderArgs).ToString();
        var indexDescription = new StringBuilder(bsonIndexFields)
            .Append(options?.Unique is true ? "unique" : string.Empty)
            .Append(options?.Sparse is true ? "sparse" : string.Empty)
            .Append(options?.WildcardProjection is { } projection ? projection.Render(bsonRenderArgs) : string.Empty)
            .Append(options?.PartialFilterExpression is { } filter ? filter.Render(bsonRenderArgs) : string.Empty)
            .ToString();

        var hashHex = ComputeIndexHash(indexDescription);
        var name = prefix + "_" + hashHex;

        if (ValidNameRegex.Match(name) is { Success: true })
        {
            indexName = new UniqueIndexName
            {
                FullName = name,
                Prefix = prefix,
                Hash = hashHex,
            };

            return true;
        }

        indexName = null;
        return false;
    }

    private static string ComputeIndexHash(string name)
    {
#if NET6_0_OR_GREATER
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(name));
        return Convert.ToHexString(hash).ToLowerInvariant().Substring(0, IndexPartialHashLength);
#else
        using (var sha = SHA256.Create())
        {
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(name));
            var hashHex = BitConverter.ToString(hash).Replace("-", string.Empty);
            return hashHex.ToLowerInvariant().Substring(0, IndexPartialHashLength);
        }
#endif
    }

    public static bool TryCreate(BsonValue indexDocument, [MaybeNullWhen(false)] out UniqueIndexName indexName)
    {
        var name = indexDocument["name"]?.AsString ?? string.Empty;

        if (ValidNameRegex.Match(name) is { Success: true } match)
        {
            indexName = new UniqueIndexName
            {
                FullName = name,
                Prefix = match.Groups["Prefix"].Value,
                Hash = match.Groups["Hash"].Value,
            };

            return true;
        }

        indexName = null;
        return false;
    }

    public override string ToString()
    {
        return this.FullName;
    }
}
using MongoDB.Driver;

namespace Workleap.Extensions.Mongo;

/// <summary>
/// Inherit from this class to define indexes for a particular document type.
/// </summary>
public abstract class MongoIndexProvider<TDocument>
    where TDocument : class
{
    public IndexKeysDefinitionBuilder<TDocument> IndexKeys => Builders<TDocument>.IndexKeys;

    public abstract IEnumerable<CreateIndexModel<TDocument>> CreateIndexModels();

    /// <summary>
    /// Override this method to define Atlas Search indexes for this document type.
    /// Atlas Search indexes are managed separately from regular indexes and require a MongoDB Atlas cluster with the mongot process.
    /// </summary>
    public virtual IEnumerable<CreateSearchIndexModel> CreateSearchIndexModels() => [];
}
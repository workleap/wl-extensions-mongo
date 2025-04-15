using System.Collections.Concurrent;
using Workleap.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Workleap.Extensions.Mongo.Security;

internal sealed class SensitiveInformationConvention : ConventionBase, IPostProcessingConvention
{
    private readonly IMongoValueEncryptor _mongoValueEncryptor;
    private readonly ConcurrentDictionary<SerializerKey, IBsonSerializer> _serializerCache;

    public SensitiveInformationConvention(IMongoValueEncryptor mongoValueEncryptor)
    {
        this._mongoValueEncryptor = mongoValueEncryptor;
        this._serializerCache = new ConcurrentDictionary<SerializerKey, IBsonSerializer>();
    }

    public void PostProcess(BsonClassMap classMap)
    {
        foreach (var memberMap in classMap.DeclaredMemberMaps)
        {
            if (SensitiveTypesCache.TryGetSensitiveInformation(memberMap.MemberInfo, out var attribute))
            {
                var existingSerializer = memberMap.GetSerializer();
                memberMap.SetSerializer(this.CreateSensitiveInformationSerializer(existingSerializer, attribute.Scope));
            }
        }
    }

    private IBsonSerializer CreateSensitiveInformationSerializer(IBsonSerializer existingSerializer, SensitivityScope sensitivityScope)
    {
        var key = new SerializerKey(existingSerializer.ValueType, sensitivityScope);

        IBsonSerializer Create(SerializerKey x)
        {
            var newSerializerType = typeof(SensitiveInformationSerializer<>).MakeGenericType(x.ValueType);
            var newSerializer = (IBsonSerializer?)Activator.CreateInstance(newSerializerType, existingSerializer, this._mongoValueEncryptor, x.SensitivityScope);
            return newSerializer ?? throw new InvalidOperationException("An error occurred while creating an instance of type " + newSerializerType);
        }

        return this._serializerCache.GetOrAdd(key, Create);
    }

    public static bool IsSensitiveType(Type type) => SensitiveTypesCache.HasSensitiveProperty(type);

    private readonly struct SerializerKey : IEquatable<SerializerKey>
    {
        public SerializerKey(Type valueType, SensitivityScope sensitivityScope)
        {
            this.ValueType = valueType;
            this.SensitivityScope = sensitivityScope;
        }

        public Type ValueType { get; }

        public SensitivityScope SensitivityScope { get; }

        public override bool Equals(object? obj)
        {
            return obj is SerializerKey other && this.Equals(other);
        }

        public bool Equals(SerializerKey other)
        {
            return this.ValueType == other.ValueType && this.SensitivityScope == other.SensitivityScope;
        }

        public override int GetHashCode()
        {
#if NET6_0_OR_GREATER
            return HashCode.Combine(this.ValueType, this.SensitivityScope);
#else
            unchecked
            {
                return (this.ValueType.GetHashCode() * 397) ^ (int)this.SensitivityScope;
            }
#endif
        }
    }
}
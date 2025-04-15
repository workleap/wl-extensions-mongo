using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace MongoDB.Driver;

public static class AsyncCursorExtensions
{
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "That's a common pattern to use the default cancellation token")]
    public static async IAsyncEnumerable<TDocument> ToAsyncEnumerable<TDocument>(
        this IAsyncCursor<TDocument> cursor,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (cursor == null)
        {
            throw new ArgumentNullException(nameof(cursor));
        }

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var document in cursor.Current)
            {
                yield return document;
            }
        }
    }

    public static async IAsyncEnumerable<TDocument> ToAsyncEnumerable<TDocument>(
        this IAsyncCursorSource<TDocument> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        using var cursor = await source.ToCursorAsync(cancellationToken).ConfigureAwait(false);

        await foreach (var document in ToAsyncEnumerable(cursor, cancellationToken))
        {
            yield return document;
        }
    }
}
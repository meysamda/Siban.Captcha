using System.Text.Json;

namespace Microsoft.Extensions.Caching.Distributed;

internal static class DistributedCacheExtentions
{
    public static Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        return cache.SetAsync(key, bytes, options, token);
    }

    public static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key, CancellationToken token = default)
    {
        try
        {
            var bytes = await cache.GetAsync(key, token);
            if (bytes == null)
                return default;

            var result = JsonSerializer.Deserialize<T>(bytes);
            return result;
        }
        catch
        {
            return default;
        }
    }
}
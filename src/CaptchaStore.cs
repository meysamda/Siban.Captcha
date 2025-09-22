using Microsoft.Extensions.Caching.Distributed;

namespace Siban.Captcha;

public class CaptchaStore : ICaptchaStore
{
    private readonly IDistributedCache _cache;
    private readonly CaptchaOptions _captchaOptions;
    private readonly DistributedCacheEntryOptions cacheOptions;

    public CaptchaStore(IDistributedCache cache, CaptchaOptions config)
    {
        _cache = cache;
        _captchaOptions = config;
        cacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_captchaOptions.ExpiresIn) };
    }

    public async Task SetCaptchaAsync(Guid id, string text, CancellationToken cancellationToken = default)
    {
        var key = GetKey(id);
        await _cache.SetAsync(key + ":text", text, cacheOptions, cancellationToken);
        await _cache.SetAsync(key + ":attempts", "0", cacheOptions, cancellationToken);
    }
    public async Task<string?> GetCaptchaTextAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = GetKey(id);
        return await _cache.GetAsync<string>(key + ":text", cancellationToken);
    }

    public async Task<int> GetCaptchaAttemptsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = GetKey(id);
        var attemptsString = await _cache.GetAsync<string>(key + ":attempts", cancellationToken);
        return int.TryParse(attemptsString, out var parsedAttempts) ? parsedAttempts : 0;
    }

    public async Task IncrementCaptchaAttemptsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = GetKey(id);
        var attemps = await GetCaptchaAttemptsAsync(id, cancellationToken);
        attemps++;
        await _cache.SetAsync(key + ":attempts", attemps.ToString(), cacheOptions, cancellationToken);
    }

    public async Task RemoveCaptchaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = GetKey(id);
        await _cache.RemoveAsync(key + ":text", cancellationToken);
        await _cache.RemoveAsync(key + ":attempts", cancellationToken);
    }

    private static string GetKey(Guid id) => $"captcha:{id}";
}
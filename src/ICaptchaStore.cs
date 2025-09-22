namespace Siban.Captcha;

public interface ICaptchaStore
{
    Task<string?> GetCaptchaTextAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetCaptchaAttemptsAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetCaptchaAsync(Guid id, string text, CancellationToken cancellationToken = default);
    Task IncrementCaptchaAttemptsAsync(Guid id, CancellationToken cancellationToken = default);
    Task RemoveCaptchaAsync(Guid id, CancellationToken cancellationToken = default);
}

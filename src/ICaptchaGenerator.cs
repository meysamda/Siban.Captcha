namespace Siban.Captcha;

public interface ICaptchaGenerator
{
    Task<CaptchaResult> GenerateAsync(CancellationToken cancellationToken = default);
}

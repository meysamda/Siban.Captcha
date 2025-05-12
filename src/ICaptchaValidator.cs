namespace Siban.Captcha;

public interface ICaptchaValidator
{
    Task<bool> ValidateAsync(Guid id, string input, CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync(string captchaData, CancellationToken cancellationToken = default);
}
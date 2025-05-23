namespace Siban.Captcha;

public class CaptchaValidator : ICaptchaValidator
{
    private readonly ICaptchaStore _store;
    private readonly CaptchaOptions _options;

    public CaptchaValidator(ICaptchaStore store, CaptchaOptions options)
    {
        _store = store;
        _options = options;
    }

    public Task<bool> ValidateAsync(string captchaData, CancellationToken cancellationToken = default)
    {      
        if (_options.IsSandboxMode)
            return Task.FromResult(captchaData.Equals(_options.SandboxKey, StringComparison.OrdinalIgnoreCase));

        var captchaDataArray = captchaData.Split("_");
        if (captchaDataArray.Length == 2)
        {
            var captchaInput = captchaDataArray[1];
            if (!string.IsNullOrEmpty(captchaInput) && Guid.TryParse(captchaDataArray[0], out Guid captchaId))
            {
                return ValidateAsync(captchaId, captchaDataArray[1], cancellationToken);
            }
        };

        return Task.FromResult(false);
    }

    public async Task<bool> ValidateAsync(Guid id, string input, CancellationToken cancellationToken = default)
    {
        var normalizedInput = PersianCharactersHelper.NormalizeToEnglishNumbers(input);

        if (_options.IsSandboxMode)
            return normalizedInput.Equals(_options.SandboxKey, StringComparison.OrdinalIgnoreCase);

        var hashedText = await _store.GetCaptchaTextAsync(id, cancellationToken);
        var attempts = await _store.GetCaptchaAttemptsAsync(id, cancellationToken);

        if (hashedText is null || attempts >= _options.MaxAttempts)
            return false;

        var hashedInput = CryptographyHelper.HashText(normalizedInput);

        if (hashedInput.Equals(hashedText, StringComparison.OrdinalIgnoreCase))
        {
            await _store.RemoveCaptchaAsync(id, cancellationToken);
            return true;
        }

        await _store.IncrementCaptchaAttemptsAsync(id, cancellationToken);
        return false;
    }
}

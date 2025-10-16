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
        string? idStr = null;
        string input = captchaData;

        var captchaDataArray = captchaData.Split("_");
        if (captchaDataArray.Length == 2)
        {
            idStr = captchaDataArray[0];
            input = captchaDataArray[1];
        }

        return ValidateAsync(idStr, input, cancellationToken);
    }

    public async Task<bool> ValidateAsync(string? idStr, string input, CancellationToken cancellationToken = default)
    {
        var normalizedInput = PersianNumbersHelper.NormalizeToEnglishNumbers(input);
        if (_options.IsSandboxMode)
        {
            var equals = normalizedInput.Equals(_options.SandboxKey, StringComparison.OrdinalIgnoreCase);
            if (equals)
                return true;
        }

        if (!Guid.TryParse(idStr, out Guid id))
            return false;

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

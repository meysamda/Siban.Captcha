namespace Siban.Captcha;

public class CaptchaOptions
{
    public int Length { get; set; }
    public required string AllowedChars { get; set; }
    public CaptchaDifficulty Difficulty { get; set; }
    public int ExpiresIn { get; set; }
    public int MaxAttempts { get; set; }

    public bool IsSandboxMode { get; set; }
    public string? SandboxKey { get; set; }
}

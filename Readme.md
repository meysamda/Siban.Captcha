# Siban.Captcha

A flexible and pluggable CAPTCHA generator for ASP.NET Core using ImageSharp and IDistributedCache.

## Features

- Generate image CAPTCHAs with configurable difficulty and character sets
- Persian and English number support
- Secure storage using distributed cache (e.g., Redis, SQL Server)
- Pluggable and easy to integrate with ASP.NET Core
- Extensible options for sandbox/testing mode

## Installation

Install via NuGet:

```sh
dotnet add package Siban.Captcha
```

## Usage

### 1. Register Services

In your `Program.cs` or `Startup.cs`:

```csharp
using Siban.Captcha;

var builder = WebApplication.CreateBuilder(args);

// Add distributed cache (e.g., in-memory for demo)
builder.Services.AddDistributedMemoryCache();

// Configure CaptchaOptions
var captchaOptions = new CaptchaOptions
{
    Length = 6,
    AllowedChars = "1234567890",
    Difficulty = CaptchaDifficulty.Medium,
    ExpiresIn = 120, // seconds
    MaxAttempts = 5,
    IsSandboxMode = false
};

builder.Services.AddCaptcha(captchaOptions);

var app = builder.Build();
```

### 2. Generate a CAPTCHA

Inject `ICaptchaGenerator` and call `GenerateAsync`:

```csharp
public class CaptchaController : ControllerBase
{
    private readonly ICaptchaGenerator _captchaGenerator;

    public CaptchaController(ICaptchaGenerator captchaGenerator)
    {
        _captchaGenerator = captchaGenerator;
    }

    [HttpGet("captcha")]
    public async Task<IActionResult> GetCaptcha()
    {
        var result = await _captchaGenerator.GenerateAsync();
        using var ms = new MemoryStream();
        result.Image.SaveAsPng(ms);
        ms.Position = 0;
        return File(ms.ToArray(), "image/png", $"{result.Id}.png");
    }
}
```

### 3. Validate a CAPTCHA

Inject `ICaptchaValidator` and call `ValidateAsync`:

```csharp
public class CaptchaValidationRequest
{
    public Guid Id { get; set; }
    public string Text { get; set; }
}

[HttpPost("captcha/validate")]
public async Task<IActionResult> ValidateCaptcha([FromBody] CaptchaValidationRequest request, [FromServices] ICaptchaValidator validator)
{
    var isValid = await validator.ValidateAsync(request.Id, request.Text);
    return Ok(new { isValid });
}
```

### 4. Using the Validation Filter (Optional)

You can use `ValidateCaptchaFilter` as an action filter in your controllers for automatic validation.

```csharp
[ServiceFilter(typeof(ValidateCaptchaFilter))]
[HttpPost("protected-action")]
public IActionResult ProtectedAction()
{
    // Your logic here
    return Ok();
}
```

## Options

See `CaptchaOptions` for all configuration options:

- `Length`: Number of characters in the CAPTCHA
- `AllowedChars`: Allowed characters for CAPTCHA text
- `Difficulty`: `Easy`, `Medium`, or `Hard` (affects noise/distortion)
- `ExpiresIn`: Expiration time in seconds
- `MaxAttempts`: Maximum allowed attempts before CAPTCHA expires
- `IsSandboxMode`: If true, always accepts `SandboxKey` as valid
- `SandboxKey`: The key to use in sandbox mode

## License

MIT License. See [LICENSE.txt](LICENSE.txt).
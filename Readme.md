# Siban.Captcha

A flexible and pluggable CAPTCHA generator for ASP.NET Core using ImageSharp and IDistributedCache.

---

## Overview

**Siban.Captcha** helps you protect your web forms and APIs from bots by generating image-based CAPTCHAs. It is designed for easy integration with ASP.NET Core and supports distributed caching for secure and scalable storage.

---

## Prerequisites

> **Important:**  
> You **must** register an implementation of `IDistributedCache` in your ASP.NET Core application.  
> This can be in-memory (for development), Redis, SQL Server, or any other supported distributed cache.  
> Without this, the CAPTCHA service will not function.

Example for in-memory cache (for demo/testing):

```csharp
builder.Services.AddDistributedMemoryCache();
```

For production, use a distributed cache like Redis:

```csharp
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = "localhost:6379";
});
```

---

## Installation

Install via NuGet:

```sh
dotnet add package Siban.Captcha
```

---

## Usage

### 1. Register the Service

In your `Program.cs` or `Startup.cs`:

```csharp
using Siban.Captcha;

var builder = WebApplication.CreateBuilder(args);

// Register your distributed cache here!
builder.Services.AddDistributedMemoryCache(); // Or Redis/SQL Server

// Configure CaptchaOptions
var captchaOptions = new CaptchaOptions
{
    Length = 6,
    AllowedChars = "1234567890",
    Difficulty = CaptchaDifficulty.Medium,
    ExpiresIn = 120, // seconds
    MaxAttempts = 5,
    IsSandboxMode = false,
    SandboxKey = "12345"
};

builder.Services.AddCaptcha(captchaOptions);

var app = builder.Build();
```

---

### 2. How CAPTCHA Generation Works (For Clients)

When a client (browser, mobile app, etc.) requests a CAPTCHA:

- The server generates a random code (e.g., 6 digits).
- An image is created showing this code, with noise/distortion based on the configured difficulty.
- The server stores the correct answer securely in the distributed cache, linked to a unique ID.
- The client receives:
  - The CAPTCHA image as a **Base64-encoded string** (recommended best practice for APIs)
  - The unique CAPTCHA ID

**Recommended Practice:**  
Returning the image as a Base64 string in JSON is a best practice for modern APIs, as it simplifies client-side handling and avoids issues with binary data over HTTP.

---

### 3. Example Controller (Recommended)

Here is a recommended controller implementation for API scenarios:

```csharp
[Route("api/captcha")]
[ApiController]
public class CaptchaController : ControllerBase
{
    private readonly ICaptchaGenerator _captchaGenerator;

    public CaptchaController(ICaptchaGenerator captchaGenerator)
    {
        _captchaGenerator = captchaGenerator;
    }

    [HttpGet]
    public async Task<IResult> GetCaptcha(CancellationToken cancellationToken = default)
    {
        var result = await _captchaGenerator.GenerateAsync(cancellationToken);

        using var stream = new MemoryStream();
        result.Image.SaveAsPng(stream);
        var base64 = Convert.ToBase64String(stream.ToArray());

        var resultObj = new
        {
            result.Id,
            Image = base64
        };

        return Results.Json(resultObj);
    }
}
```

**Client Usage:**  
- Call `GET /api/captcha`
- Receive a JSON response:
  ```json
  {
    "id": "b1a2c3d4-5678-90ab-cdef-1234567890ab",
    "image": "iVBORw0KGgoAAAANSUhEUgAA..."
  }
  ```
- Show the image by setting the `src` of an `<img>` tag to `data:image/png;base64,{image}`

---

### 4. How CAPTCHA Validation Works (For Clients)

#### 4.1 Using ValidateCaptchaFilter (Recommended)

The `ValidateCaptchaFilter` provides automatic CAPTCHA validation for your endpoints. The filter expects a single string value in the format: `{captchaId}_{userInput}`
note: The value should be a string where the CAPTCHA ID and the user input are joined by an underscore (`_`).
- Example: `"b1a2c3d4-5678-90ab-cdef-1234567890ab_123456"`
#### How Captcha Validation Data is Extracted

When using the <c>ValidateCaptchaFilter</c> in your application, the filter automatically extracts the captcha validation data from the incoming HTTP request based on the request method and content type:

- **POST/PUT Requests:**
  - If the request has a form content type, the captcha data is read from the form field with the configured name (default: <c>captchaData</c>).
  - If the request content type is <c>application/json</c>, the filter reads the request body and extracts the captcha data from the corresponding JSON property.

- **GET Requests:**
  - The filter first checks the query string for the captcha data field.
  - If not found in the query string, it looks for the <c>X-Captcha-Data</c> header

If the captcha data is missing or invalid, the filter throws an exception and the request will not proceed to the action method. This ensures that only requests with valid captcha data are processed by your endpoints.

**Example:**
- For form submissions, include a field named <c>captchaData</c>.
- For JSON requests, include a property named <c>captchaData</c> in the request body.
- For GET requests, provide <c>?captchaData=...</c> in the query string or set the <c>X-Captcha-Data</c> header.

**Example usage in controller:**

```csharp
[ServiceFilter(typeof(ValidateCaptchaFilter), Arguments = ["captchaData"])]
[HttpPost("protected-action")]
public IActionResult ProtectedAction()
{
    // Your protected action logic here
    return Ok();
}
```
- The filter will automatically extract and validate the CAPTCHA using the provided value.
- If validation fails, the request will be rejected.

#### 4.2 Using Direct Validation

For more control, you can use `ICaptchaValidator` directly:

```csharp
// Method 1: Combined format
await validator.ValidateAsync("b1a2c3d4-5678-90ab-cdef-1234567890ab_123456");

// Method 2: Separate ID and input
await validator.ValidateAsync("b1a2c3d4-5678-90ab-cdef-1234567890ab", "123456");
```

**Validation Process:**
- Automatically normalizes Persian numbers to English numbers
- Checks sandbox mode (if enabled)
- Validates GUID format
- Checks for CAPTCHA existence and expiration
- Verifies maximum attempts not exceeded
- Compares hashed input with stored value
- Removes CAPTCHA on success or increments attempt counter on failure

---

## CaptchaOptions

- `Length`: Number of characters in the CAPTCHA
- `AllowedChars`: Allowed characters for CAPTCHA text
- `Difficulty`: `Easy`, `Medium`, or `Hard` (affects noise/distortion)
- `ExpiresIn`: Expiration time in seconds
- `MaxAttempts`: Maximum allowed attempts before CAPTCHA expires
- `IsSandboxMode`: If true, always accepts `SandboxKey` as valid
- `SandboxKey`: The key to use in sandbox mode

---

## License

MIT License. See [LICENSE.txt](LICENSE.txt).
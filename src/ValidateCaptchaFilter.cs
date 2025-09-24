using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Siban.Captcha;

public class ValidateCaptchaFilter : IAsyncActionFilter
/// <summary>
/// ## How Captcha Validation Data is Extracted
///
/// When using the <c>ValidateCaptchaFilter</c> in your application, the filter automatically extracts the captcha validation data from the incoming HTTP request based on the request method and content type:
///
/// - **POST/PUT Requests:**
///   - If the request has a form content type, the captcha data is read from the form field with the configured name (default: <c>captchaData</c>).
///   - If the request content type is <c>application/json</c>, the filter reads the request body and extracts the captcha data from the corresponding JSON property.
/// - **GET Requests:**
///   - The filter first checks the query string for the captcha data field.
///   - If not found in the query string, it looks for the <c>X-Captcha-Data</c> header.
///
/// If the captcha data is missing or invalid, the filter throws an exception and the request will not proceed to the action method. This ensures that only requests with valid captcha data are processed by your endpoints.
///
/// **Example:**
/// - For form submissions, include a field named <c>captchaData</c>.
/// - For JSON requests, include a property named <c>captchaData</c> in the request body.
/// - For GET requests, provide <c>?captchaData=...</c> in the query string or set the <c>X-Captcha-Data</c> header.
/// </summary>
{
    private readonly ICaptchaValidator _captchaValidator;
    private readonly string _captchaDataFieldName;

    public ValidateCaptchaFilter(ICaptchaValidator captchaValidator, string captchaDataFieldName = "captchaData")
    {
        _captchaDataFieldName = captchaDataFieldName;
        _captchaValidator = captchaValidator;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var captchaData = await ReadCaptchaDataAsync(context);
        if (string.IsNullOrEmpty(captchaData))
        {
            throw new ArgumentNullException(_captchaDataFieldName);
        }

        var isValidCaptcha = await _captchaValidator.ValidateAsync(captchaData, context.HttpContext.RequestAborted);
        if (!isValidCaptcha)
        {
            throw new ArgumentException("invalidCaptcha", _captchaDataFieldName);
        }

        await next();
    }

    private async Task<string?> ReadCaptchaDataAsync(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        string? captchaData = null;

        if (request.Method == HttpMethods.Post || request.Method == HttpMethods.Put)
        {
            if (request.HasFormContentType)
            {
                captchaData = request.Form[_captchaDataFieldName];
            }
            else if (request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                request.EnableBuffering();
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;

                var json = JsonDocument.Parse(body);
                if (json.RootElement.TryGetProperty(_captchaDataFieldName, out var tokenElement))
                {
                    captchaData = tokenElement.GetString();
                }
            }
        }
        else if (request.Method == HttpMethods.Get)
        {
            // Try query string or header
            captchaData = request.Query[_captchaDataFieldName];
            if (string.IsNullOrWhiteSpace(captchaData))
                captchaData = request.Headers["X-Captcha-Data"];
        }

        return captchaData;
    }
}

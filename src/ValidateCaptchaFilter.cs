using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Siban.Captcha;

public class ValidateCaptchaFilter : IAsyncActionFilter
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

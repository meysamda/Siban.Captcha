using Siban.Captcha;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddSibanCaptcha(this IServiceCollection services, CaptchaOptions CaptchaOptions)
    {
        ArgumentNullException.ThrowIfNull(CaptchaOptions);

        services.AddSingleton(CaptchaOptions);

        services.AddScoped<ICaptchaGenerator, CaptchaGenerator>();
        services.AddScoped<ICaptchaValidator, CaptchaValidator>();

        // requires that distributed cache is registered
        services.AddScoped<ICaptchaStore, CaptchaStore>();

        services.AddScoped<ValidateCaptchaFilter>();

        return services;
    }
}
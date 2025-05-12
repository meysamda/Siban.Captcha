using SixLabors.ImageSharp;

namespace Siban.Captcha;

public class CaptchaResult
{
    public Guid Id { get; }
    public string Text { get; }
    public Image Image { get; }

    public CaptchaResult(Guid id, string text, Image image)
    {
        Id = id;
        Text = text;
        Image = image;
    }
}
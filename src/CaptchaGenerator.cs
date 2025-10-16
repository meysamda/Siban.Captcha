using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using System.Security.Cryptography;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Advanced;
using System.Reflection.Metadata.Ecma335;

namespace Siban.Captcha;

public class CaptchaGenerator : ICaptchaGenerator
{
    private readonly ICaptchaStore _store;
    private readonly CaptchaOptions _options;
    private readonly RandomNumberGenerator randomNumberGenerator;

    public CaptchaGenerator(ICaptchaStore store, CaptchaOptions options)
    {
        _options = options;
        _store = store;
        randomNumberGenerator = RandomNumberGenerator.Create();
    }

    public async Task<CaptchaResult> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var text = GenerateRandomText();
        var image = DrawImage(text);
        var result = new CaptchaResult(id, text, image);

        var normalizedText = PersianNumbersHelper.NormalizeToEnglishNumbers(text);
        var hashedText = CryptographyHelper.HashText(normalizedText);
        await _store.SetCaptchaAsync(id, hashedText, cancellationToken);

        return result;
    }

    // =================================================================

    private string GenerateRandomText()
    {
        var chars = _options.AllowedChars;
        return new string(Enumerable.Range(0, _options.Length)
            .Select(_ => chars[RandomNumber(chars.Length)]).ToArray());
    }

    private int RandomNumber(int max)
    {
        var bytes = new byte[4];
        randomNumberGenerator.GetBytes(bytes);
        return (int)(BitConverter.ToUInt32(bytes, 0) % (uint)max);
    }

    private Image<Rgba32> DrawImage(string text)
    {
        var collection = new FontCollection();
        var fontFamily = collection.Add(_options.Font);
        var font = new Font(fontFamily, 36);

        var img = new Image<Rgba32>(200, 70);

        img.Mutate(ctx =>
        {
            ctx.Fill(Color.White);
            var rect = new RectangleF(0, 0, img.Width, img.Height);
            DrawCenteredText(ctx, text, font, Color.Black, rect);
        });

        if (_options.Difficulty >= CaptchaDifficulty.Medium)
        {
            DrawNoise(img);
        }

        if (_options.Difficulty == CaptchaDifficulty.Hard)
        {
            var distorted = ApplyWaveDistortion(img);
            img.Dispose(); // prevent memory leak from old image
            img = distorted;

            img.Mutate(x => x.GaussianBlur(1.5f));
        }

        return img;
    }

    private void DrawCenteredText(IImageProcessingContext ctx, string text, Font font, Color color, RectangleF bounds)
    {
        // Measure the size of the text
        var textSize = TextMeasurer.MeasureSize(text, new TextOptions(font));

        // Calculate the position to center the text
        var position = new PointF(
            bounds.Left + (bounds.Width - textSize.Width) / 2,
            bounds.Top + (bounds.Height - textSize.Height) / 2
        );

        ctx.DrawText(text, font, color, position);
    }


    private void DrawNoise(Image<Rgba32> image)
    {
        var random = new Random();
        var pen = Pens.Solid(Color.Gray, 2);
        var lines = random.Next(8) + 2;

        image.Mutate(ctx =>
        {
            for (int i = 0; i < lines; i++)
            {
                var start = new PointF(random.Next(image.Width), random.Next(image.Height));
                var end = new PointF(random.Next(image.Width), random.Next(image.Height));
                ctx.DrawLine(pen, start, end);
            }
        });
    }

    private Image<Rgba32> ApplyWaveDistortion(Image<Rgba32> source)
    {
        int width = source.Width;
        int height = source.Height;
        var result = new Image<Rgba32>(width, height);

        float amplitude = 10f;
        float frequency = 2f * MathF.PI / 60f;

        for (int y = 0; y < height; y++)
        {
            var srcRow = source.DangerousGetPixelRowMemory(y).Span;
            int offsetX = (int)(MathF.Sin(y * frequency) * amplitude);

            for (int x = 0; x < width; x++)
            {
                int newX = x + offsetX;
                if (newX >= 0 && newX < width)
                {
                    result[newX, y] = srcRow[x];
                }
            }
        }

        return result;
    }
}
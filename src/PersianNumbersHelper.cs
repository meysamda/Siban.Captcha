namespace Siban.Captcha;

public static class PersianNumbersHelper
{
    public static string ConvertToPersian(string text)
    {
        var persianNumbers = new[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
        var englishNumbers = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        for (int i = 0; i < englishNumbers.Length; i++)
        {
            text = text.Replace(englishNumbers[i].ToString(), persianNumbers[i].ToString());
        }

        return text;
    }

    public static string NormalizeToEnglishNumbers(string input)
    {
        return input
            .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2')
            .Replace('۳', '3').Replace('۴', '4').Replace('۵', '5')
            .Replace('۶', '6').Replace('۷', '7').Replace('۸', '8')
            .Replace('۹', '9');
    }
}
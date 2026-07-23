using System.Text.RegularExpressions;

public static class PhoneValidator
{
    public static bool IsValid(string phone)
    {
        phone = RemoveFormatting(phone);

        if (phone.Length != 11)
            return false;

        if (phone.Distinct().Count() == 1)
            return false;

        var ddd = int.Parse(phone.Substring(0, 2));
        if (ddd < 11 || ddd > 99)
            return false;

        if (phone[2] != '9')
            return false;

        return true;
    }

    public static string RemoveFormatting(string phone)
    {
        return Regex.Replace(phone ?? "", "[^0-9]", "");
    }
}

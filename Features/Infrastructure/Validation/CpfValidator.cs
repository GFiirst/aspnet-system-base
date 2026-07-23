using System.Text.RegularExpressions;

public static class CpfValidator
{
    public static bool IsValid(string cpf)
    {
        cpf = RemoveFormatting(cpf);

        if (cpf.Length != 11)
            return false;

        if (cpf.Distinct().Count() == 1)
            return false;

        var digits = cpf.Select(c => int.Parse(c.ToString())).ToArray();
        
        var sum1 = 0;
        for (int i = 0; i < 9; i++)
        {
            sum1 += digits[i] * (10 - i);
        }
        
        var remainder1 = sum1 % 11;
        var digit1 = remainder1 < 2 ? 0 : 11 - remainder1;
        
        if (digits[9] != digit1)
            return false;

        var sum2 = 0;
        for (int i = 0; i < 10; i++)
        {
            sum2 += digits[i] * (11 - i);
        }
        
        var remainder2 = sum2 % 11;
        var digit2 = remainder2 < 2 ? 0 : 11 - remainder2;
        
        return digits[10] == digit2;
    }

    public static string RemoveFormatting(string cpf)
    {
        return Regex.Replace(cpf ?? "", "[^0-9]", "");
    }
}

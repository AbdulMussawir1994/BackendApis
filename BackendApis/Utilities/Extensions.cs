namespace BackendApis.Utilities;

public static class Extensions
{
    public static string GenerateUniqueNumber()
    {
        // Use a combination of timestamp and random number for uniqueness
        Random random = new Random();
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"); // Timestamp part

        // Random part with 9 digits
        string randomNumber = random.Next(100000000, 1000000000).ToString(); // Generates a number between 100,000,000 and 999,999,999

        // Concatenate timestamp and random number
        string uniqueNumber = timestamp + randomNumber;

        // Ensure the length is exactly 15 digits (in case randomNumber was less than 9 digits)
        if (uniqueNumber.Length > 12)
        {
            uniqueNumber = uniqueNumber.Substring(0, 12);
        }
        else if (uniqueNumber.Length < 12)
        {
            uniqueNumber = uniqueNumber.PadRight(12, '0');
        }

        return uniqueNumber;
    }
    public static string GetOrdinalSuffix(int number)
    {
        if (number <= 0) return number.ToString(); // Negative or zero numbers handled as simple numbers

        // Handle 11th, 12th, and 13th specifically
        if (number % 100 >= 11 && number % 100 <= 13)
        {
            return number + "th";
        }

        // Otherwise handle other numbers
        switch (number % 10)
        {
            case 1: return number + "st";
            case 2: return number + "nd";
            case 3: return number + "rd";
            default: return number + "th";
        }
    }
    public static string GetOrdinalSuffixWithoutNumber(this int number)
    {
        if (number <= 0) return number.ToString(); // Negative or zero numbers handled as simple numbers

        // Handle 11th, 12th, and 13th specifically
        if (number % 100 >= 11 && number % 100 <= 13)
        {
            return number + "th";
        }

        // Otherwise handle other numbers
        switch (number % 10)
        {
            case 1: return "st";
            case 2: return "nd";
            case 3: return "rd";
            default: return "th";
        }
    }

    public static string ConvertToWords(this decimal? number)
    {
        if (!number.HasValue)
            return "ZERO";

        decimal num = number.Value;

        if (num == 0)
            return "ZERO";

        if (num < 0)
            return "MINUS " + ConvertToWords(Math.Abs(num));

        string words = "";

        // Handle billions
        if ((int)(num / 1000000000) > 0)
        {
            words += ConvertToWords((int)(num / 1000000000)) + " BILLION ";
            num %= 1000000000;
        }

        // Handle millions
        if ((int)(num / 1000000) > 0)
        {
            words += ConvertToWords((int)(num / 1000000)) + " MILLION ";
            num %= 1000000;
        }

        // Handle thousands
        if ((int)(num / 1000) > 0)
        {
            words += ConvertToWords((int)(num / 1000)) + " THOUSAND ";
            num %= 1000;
        }

        // Handle hundreds
        if ((int)(num / 100) > 0)
        {
            words += ConvertToWords((int)(num / 100)) + " HUNDRED ";
            num %= 100;
        }

        // Handle tens and ones
        if (num > 0)
        {
            if (words != "")
                words += "AND ";

            var unitsMap = new[] { "ZERO", "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX", "SEVEN", "EIGHT", "NINE", "TEN",
       "ELEVEN", "TWELVE", "THIRTEEN", "FOURTEEN", "FIFTEEN", "SIXTEEN", "SEVENTEEN", "EIGHTEEN", "NINETEEN" };
            var tensMap = new[] { "ZERO", "TEN", "TWENTY", "THIRTY", "FORTY", "FIFTY", "SIXTY", "SEVENTY", "EIGHTY", "NINETY" };

            if (num < 20)
                words += unitsMap[(int)num];
            else
            {
                words += tensMap[(int)(num / 10)];
                if ((num % 10) > 0)
                    words += "-" + unitsMap[(int)(num % 10)];
            }
        }

        return words.Trim().ToUpper();
    }
}

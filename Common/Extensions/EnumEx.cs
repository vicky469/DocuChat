using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Common.Extensions;

public static class EnumEx
{
    public static string GetDescription<TEnum>(this TEnum enumValue) where TEnum : struct
    {
        var type = typeof(TEnum);
        if (!type.IsEnum) throw new ArgumentException("Type must be an enum");
        var memberInfo = type.GetMember(enumValue.ToString());
        if (memberInfo.Length <= 0) return enumValue.ToString();
        var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attrs.Length > 0 ? ((DescriptionAttribute)attrs[0]).Description : enumValue.ToString();
    }
    
    // Implements a fuzzy search in the TryGetEnumFromDescription method
    // Using the Levenshtein distance algorithm and other algorithms to measure string similarity.
    public static TEnum? TryGetEnumFromDescription<TEnum>(string description) where TEnum : struct
{
    var enumType = typeof(TEnum);
    if (!enumType.IsEnum) return null;
    var potentialMatches = new List<(TEnum, double)>();
    // Split the description into words
    var descriptionWords = Regex.Split(description, @"[\s.,;&'-]+").Where(x => !string.IsNullOrEmpty(x)).ToArray();
    foreach (var name in Enum.GetNames(enumType))
    {
        var enumMemberAttribute = ((DescriptionAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(DescriptionAttribute), false)).SingleOrDefault();
        if (enumMemberAttribute != null)
        {
            // Split the enum description into words
            var enumDescriptionWords = Regex.Split(enumMemberAttribute.Description, @"[\s.,;&'-]+").Where(x => !string.IsNullOrEmpty(x)).ToArray();
            
            // Find the common words between the input description and the enum description
            var commonWords = descriptionWords.Intersect(enumDescriptionWords).ToArray();
            var commonDescription = string.Join(" ", commonWords);

            // Clean and normalize the common description
            var cleanedCommonDescription = CleanDescription(commonDescription);
            cleanedCommonDescription = NormalizeDescription(cleanedCommonDescription);
            
            var similarity = CalculateSimilarity(cleanedCommonDescription, enumMemberAttribute.Description.ToLower());
            if(similarity == 1) return (TEnum)Enum.Parse(enumType, name);

            // Add the enum value and its similarity score to the list of potential matches
            potentialMatches.Add(((TEnum)Enum.Parse(enumType, name), similarity));
        }
    }

    // Return the enum value with the highest similarity score
    if (potentialMatches.Count > 0)
    {
        var bestMatch = potentialMatches.OrderByDescending(match => match.Item2).First();
        return bestMatch.Item1;
    }

    return null;
}

    private static string CleanDescription(string description) => System.Net.WebUtility.HtmlDecode(description);
    
    private static string NormalizeDescription(string description)
    {
        // Convert to lower case
        description = description.ToLower();

        // Remove special characters
        description = Regex.Replace(description, "[^a-z0-9 ]", "");

        return description;
    }

    private static double CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 1.0 : 0.0;
        }

        if (string.IsNullOrEmpty(target))
        {
            return 0.0;
        }

        var stepsToSame = ComputeLevenshteinDistance(source, target);
        return 1.0 - (double)stepsToSame / (double)Math.Max(source.Length, target.Length);
    }

    private static int ComputeLevenshteinDistance(string source, string target)
    {
        var sourceLength = source.Length;
        var targetLength = target.Length;
        var distance = new int[sourceLength + 1, targetLength + 1];

        // Initialize the distance matrix
        for (var i = 0; i <= sourceLength; distance[i, 0] = i++) ;
        for (var j = 0; j <= targetLength; distance[0, j] = j++) ;

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[sourceLength, targetLength];
    }
}
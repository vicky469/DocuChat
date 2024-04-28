using System.ComponentModel;

namespace DocumentAPI.Common.Extensions;

public static class EnumEx
{
    public static T? TryGetEnumFromDescription<T>(string description) where T : struct, Enum
    {
        var normalizedDescription = NormalizeDescription(description);

        foreach (var field in typeof(T).GetFields())
            if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
            {
                var normalizedAttributeDescription = NormalizeDescription(attribute.Description);

                // Check if the enum description contains the provided description or vice versa
                if (normalizedAttributeDescription.Contains(normalizedDescription) ||
                    normalizedDescription.Contains(normalizedAttributeDescription)) return (T)field.GetValue(null);
            }
            else
            {
                if (field.Name == description) return (T)field.GetValue(null);
            }

        return null;
    }

    public static string GetDescription<TEnum>(this TEnum enumValue) where TEnum : struct
    {
        var type = typeof(TEnum);
        if (!type.IsEnum) throw new ArgumentException("Type must be an enum");
        var memberInfo = type.GetMember(enumValue.ToString());
        if (memberInfo.Length <= 0) return enumValue.ToString();
        var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attrs.Length > 0 ? ((DescriptionAttribute)attrs[0]).Description : enumValue.ToString();
    }

    private static string NormalizeDescription(string description)
    {
        // Remove special characters, trim whitespace, and convert to lower case
        var normalizedDescription = new string(description
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray())
            .Trim()
            .ToLower();

        return normalizedDescription;
    }
}
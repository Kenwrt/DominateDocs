using System.ComponentModel;
using System.Reflection;
using static EnumExtensions;
using static DominateDocsData.Enums.UserEnums;

public static class EnumExtensions
{
    /// <summary>
    /// Returns [Description("...")] if present; otherwise returns the enum member name.
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name is null) return value.ToString();

        var field = type.GetField(name);
        if (field is null) return name;

        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? name;
    }
}

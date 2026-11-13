using System.Collections;
using System.Reflection;

namespace PokeBinder.Emails.Rendering;

/// <summary>How a model's types look from inside a Liquid template.</summary>
internal static class ModelTypes
{
    /// <summary>A type Fluid turns into a Liquid value of its own: no members a template can name.</summary>
    public static bool IsScalar(Type type)
    {
        type = Unwrap(type);
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly)
            || type == typeof(TimeOnly) || type == typeof(TimeSpan) || type == typeof(Guid)
            || type == typeof(Uri) || type == typeof(object);
    }

    /// <summary>The element type of a list, or null for a type that is not one.</summary>
    public static Type? ElementType(Type type)
    {
        type = Unwrap(type);
        if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type)) return null;
        if (type.IsArray) return type.GetElementType();
        return type.GetInterfaces().Append(type)
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0] ?? typeof(object);
    }

    /// <summary>A public property by the name a template uses for it, which ignores case.</summary>
    public static PropertyInfo? Property(Type type, string name) =>
        Unwrap(type).GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

    public static Type Unwrap(Type type) => Nullable.GetUnderlyingType(type) ?? type;
}

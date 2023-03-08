using System.Reflection;

namespace NibusConsulting.DotNetObjectComparer;

internal static class PropertyInfoExtensions
{
    internal static object? TryGetValue(this PropertyInfo propertyInfo, object? value)
    {
        try
        {
            return value is null ? null : propertyInfo.GetValue(value, null);
        }
        catch
        {
            return default;
        }
    }
}

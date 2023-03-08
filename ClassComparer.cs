using System.Collections;
using System.Reflection;

namespace NibusConsulting.DotNetObjectComparer;

public sealed class ClassComparer
{
    public static Dictionary<string, Values> GetDelta<T>(T oldObject, T newObject, string previousNode = "") where T : class?, new()
    {
        return GetDelta(typeof(T), oldObject, newObject, previousNode);
    }

    private static Dictionary<string, Values> GetDelta(Type type, object? oldObject, object? newObject, string previousNode = "", bool isFromArray = false, PropertyInfo arrayProperty = default)
    {
        var resultDict = new Dictionary<string, Values>();

        if (isFromArray)
        {
            RunComparerByType(previousNode, resultDict, arrayProperty, type, oldObject, newObject);
        }
        else
        {
            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;

                var oldValue = property.TryGetValue(oldObject);
                var newValue = property.TryGetValue(newObject);
                RunComparerByType(previousNode, resultDict, property, propertyType, oldValue, newValue);
            }
        }

        return resultDict;
    }

    private static void RunComparerByType(string previousNode, Dictionary<string, Values> resultDict, PropertyInfo property, Type propertyType, object? oldValue, object? newValue)
    {
        if (propertyType.IsGenericType)
        {
            if (propertyType.GetInterfaces().Any(i => i == typeof(IEnumerable)))
            {
                var oldValueAsEnumerable = (((IEnumerable?)oldValue) ?? new List<object>()).Cast<object>();
                var newValueAsEnumerable = (((IEnumerable?)newValue) ?? new List<object>()).Cast<object>();

                var maxSize = Math.Max(oldValueAsEnumerable!.Count(), newValueAsEnumerable!.Count());

                for (int i = 0; i < maxSize; i++)
                {
                    var oldItemValue = oldValueAsEnumerable.ElementAtOrDefault(i);
                    var newItemValue = newValueAsEnumerable.ElementAtOrDefault(i);

                    Type? itemValueType = oldItemValue != null
                        ? oldItemValue.GetType()
                        : newItemValue != null
                            ? newItemValue.GetType() : null;

                    if (itemValueType!.IsValueType || itemValueType == typeof(string))
                    {
                        GetDelta(
                            typeof(string),
                            string.Join(',', oldValueAsEnumerable.OrderBy(x => x).Select(item => item.ToString())),
                            string.Join(',', newValueAsEnumerable.OrderBy(x => x).Select(item => item.ToString())),
                            $"{(!string.IsNullOrEmpty(previousNode) ? $"{previousNode}." : string.Empty)}{property.Name}",
                            true,
                            property
                        )
                            .ToList().ForEach(item =>
                            {
                                resultDict.Add(item.Key, item.Value);
                            });

                        break;
                    }
                    else
                    {
                        RunComparerByType($"{(!string.IsNullOrEmpty(previousNode) ? $"{previousNode}." : string.Empty)}{property.Name}[{i}]", resultDict, property, itemValueType!, oldItemValue, newItemValue);
                    }
                }
            }
        }
        else if (propertyType.IsValueType)
        {
            Compare(oldValue, newValue, previousNode, resultDict, property);
        }
        else if (propertyType.IsClass && !(propertyType == typeof(string)))
        {
            CompareClass(oldValue, newValue, previousNode, resultDict, property, propertyType);
        }
        else
        {
            Compare(oldValue, newValue, previousNode, resultDict, property);
        }
    }

    private static void CompareClass(object? oldValue, object? newValue, string previousNode, Dictionary<string, Values> resultDict, PropertyInfo property, Type propertyType)
    {
        var prefix = $"{(!string.IsNullOrEmpty(previousNode) ? $"{previousNode}." : string.Empty)}";

        GetDelta(propertyType, oldValue, newValue, $"{prefix}{property.Name}").ToList().ForEach(item =>
        {
            resultDict.Add(item.Key, item.Value);
        });
    }

    private static void Compare(object? oldValue, object? newValue, string previousNode, Dictionary<string, Values> resultDict, PropertyInfo property)
    {
        if (!Equals(oldValue, newValue))
        {
            var prefix = $"{(!string.IsNullOrEmpty(previousNode) ? $"{previousNode}." : string.Empty)}";
            resultDict.Add($"{prefix}{property.Name}", new Values { OldValue = $"{oldValue}", NewValue = $"{newValue}" });
        }
    }
}

namespace Persistence.Context;

internal static class TypeExtensions
{
    public static bool IsAssignableFromGeneric(this Type genericType, Type givenType)
    {
        while (givenType != null && givenType != typeof(object))
        {
            var current = givenType.IsGenericType
                ? givenType.GetGenericTypeDefinition()
                : givenType;

            if (genericType == current)
                return true;

            givenType = givenType.BaseType!;
        }

        return false;
    }
}

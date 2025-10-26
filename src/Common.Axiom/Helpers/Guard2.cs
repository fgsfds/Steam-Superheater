using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;

namespace Common.Axiom.Helpers;

public static class Guard2
{
    /// <summary>
    /// Asserts that the input value is of a specific type and outputs this value cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <param name="value">The input <see cref="object"/> to test.</param>
    /// <param name="ret">The output <see cref="object"/> cast to the type <see param="T"/>.</param>
    /// <param name="name">The name of the input parameter being tested.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not of type <typeparamref name="T"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsOfType<T>([NotNull] object? value, [NotNull] out T? ret, [CallerArgumentExpression(nameof(value))] string name = "")
    {
        if (value == null)
        {
            ThrowHelper.ThrowArgumentNullException();
        }

        if (value.GetType() == typeof(T))
        {
            ret = (T)value;
            return;
        }

        ret = default;
        ThrowHelper.ThrowArgumentException();
    }
}

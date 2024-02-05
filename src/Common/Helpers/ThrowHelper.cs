using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Common.Helpers
{
    /// <summary>
    /// Public implementation of System.ThrowHelper class
    /// </summary>
    public static class ThrowHelper
    {
        [DoesNotReturn]
        public static void BackwardsCompatibilityException(string? message = "") => throw new BackwardsCompatibilityException(message);

        [DoesNotReturn]
        public static T BackwardsCompatibilityException<T>(string? message = "") => throw new BackwardsCompatibilityException(message);

        [DoesNotReturn]
        public static void HashCheckFailedException(string? message = "") => throw new HashCheckFailedException(message);

        [DoesNotReturn]
        public static T HashCheckFailedException<T>(string? message = "") => throw new HashCheckFailedException(message);

        [DoesNotReturn]
        public static void NotImplementedException(string? message = "") => throw new NotImplementedException(message);

        [DoesNotReturn]
        public static T NotImplementedException<T>(string? message = "") => throw new NotImplementedException(message);

        [DoesNotReturn]
        public static void Exception(string? message = "") => throw new Exception(message);

        [DoesNotReturn]
        public static T Exception<T>(string? message = "") => throw new Exception(message);

        [DoesNotReturn]
        public static void FileNotFoundException(string? message = "") => throw new FileNotFoundException(message);

        [DoesNotReturn]
        public static T FileNotFoundException<T>(string? message = "") => throw new FileNotFoundException(message);

        [DoesNotReturn]
        public static void ArgumentNullException(string? message = "") => throw new ArgumentNullException(message);

        [DoesNotReturn]
        public static T ArgumentNullException<T>(string? message = "") => throw new ArgumentNullException(message);

        [DoesNotReturn]
        public static void ArgumentOutOfRangeException(string? message = "") => throw new ArgumentOutOfRangeException(message);

        [DoesNotReturn]
        public static T ArgumentOutOfRangeException<T>(string? message = "") => throw new ArgumentOutOfRangeException(message);

        [DoesNotReturn]
        public static void PlatformNotSupportedException(string? message = "") => throw new PlatformNotSupportedException(message);

        [DoesNotReturn]
        public static T PlatformNotSupportedException<T>(string? message = "") => throw new PlatformNotSupportedException(message);

        [DoesNotReturn]
        public static void NullReferenceException(string? message = "") => throw new NullReferenceException(message);

        [DoesNotReturn]
        public static T NullReferenceException<T>(string? message = "") => throw new NullReferenceException(message);

        [DoesNotReturn]
        public static void ArgumentException(string? message = "") => throw new ArgumentException(message);

        [DoesNotReturn]
        public static T ArgumentException<T>(string? message = "") => throw new ArgumentException(message);
    }

    public static class Guard
    {
        /// <summary>
        /// Throw <see cref="NullReferenceException"/> if <paramref name="obj"/> is null
        /// </summary>
        public static void ThrowIfNull([NotNull] this object? obj, [CallerArgumentExpression(nameof(obj))] string? name = null)
        {
            if (obj is null)
            {
                throw new NullReferenceException(name);
            }
        }

        /// <summary>
        /// Throw <see cref="ArgumentException"/> if <paramref name="obj"/> is not of type <typeparamref name="T"/>
        /// otherwise, return <paramref name="obj"/> cast to <typeparamref name="T"/>
        /// </summary>
        public static void ThrowIfNotType<T>([NotNull] this object? obj, out T ret, [CallerArgumentExpression(nameof(obj))] string? name = null)
        {
            if (obj is not T objT)
            {
                throw new ArgumentException(name);
            }

            ret = objT; 
        }

        /// <summary>
        /// Throw <see cref="NullReferenceException"/> if string is null or empty
        /// </summary>
        public static void ThrowIfNullOrEmpty([NotNull] this string? obj, [CallerArgumentExpression(nameof(obj))] string? name = null)
        {
            if (string.IsNullOrEmpty(obj))
            {
                throw new NullReferenceException(name);
            }
        }
    }
}

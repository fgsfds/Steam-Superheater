namespace Common.Helpers
{
    /// <summary>
    /// Operation result
    /// </summary>
    public readonly struct Result(
        ResultEnum resultEnum,
        string message
        )
    {
        /// <summary>
        /// Operation result enum
        /// </summary>
        private readonly ResultEnum ResultEnum = resultEnum;

        /// <summary>
        /// Operation result message
        /// </summary>
        public readonly string Message = message;

        /// <summary>
        /// Is operation successful
        /// </summary>
        public bool IsSuccess => ResultEnum is ResultEnum.Success;

        public override bool Equals(object? obj)
        {
            switch (obj)
            {
                case Result result:
                    return ResultEnum == result.ResultEnum;
                case ResultEnum resultE:
                    return ResultEnum == resultE;
                default:
                    ThrowHelper.ArgumentException($"Can't compare Result to {obj?.GetType()}");
                    return false;
            }
        }

        public static bool operator == (Result obj1, ResultEnum obj2)
        {
            return obj1.ResultEnum == obj2;
        }

        public static bool operator != (Result obj1, ResultEnum obj2)
        {
            return obj1.ResultEnum != obj2;
        }

        public override int GetHashCode()
        {
            ThrowHelper.NotImplementedException(string.Empty);
            return 0;
        }
    }

    public enum ResultEnum : byte
    {
        /// <summary>
        /// Successful operation
        /// </summary>
        Success,
        /// <summary>
        /// Error while validating MD5
        /// </summary>
        MD5Error,
        /// <summary>
        /// File or directory not found
        /// </summary>
        NotFound,
        /// <summary>
        /// Connection to online resource failed
        /// </summary>
        ConnectionError,
        /// <summary>
        /// Access to file failed
        /// </summary>
        FileAccessError,
        /// <summary>
        /// General error
        /// </summary>
        Error,
        /// <summary>
        /// Backwards compatibility error
        /// </summary>
        [Obsolete("Remove in version 1.0")]
        BackwardsCompatibility
    }
}

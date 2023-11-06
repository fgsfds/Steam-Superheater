namespace Common.Helpers
{
    /// <summary>
    /// Operation result
    /// </summary>
    public readonly struct Result
    {
        public Result(
            ResultEnum resultEnum,
            string message
            )
        {
            ResultEnum = resultEnum;
            Message = message;
        }

        /// <summary>
        /// Operation result enum
        /// </summary>
        public ResultEnum ResultEnum { get; init; }

        /// <summary>
        /// Operation result message
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// Is operation successful
        /// </summary>
        public bool IsSuccess => ResultEnum is ResultEnum.Ok;
    }

    public enum ResultEnum
    {
        /// <summary>
        /// Successful operation
        /// </summary>
        Ok,
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
        Error
    }
}

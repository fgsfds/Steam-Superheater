using System.Diagnostics.CodeAnalysis;

namespace Common.Axiom;

/// <summary>
/// Operation result
/// </summary>
public readonly struct Result
{
    /// <summary>
    /// Operation result enum
    /// </summary>
    public readonly ResultEnum ResultEnum;

    /// <summary>
    /// Operation result message
    /// </summary>
    public readonly string Message;

    /// <summary>
    /// Is operation successful
    /// </summary>
    public bool IsSuccess => ResultEnum is ResultEnum.Success;


    public Result(
        ResultEnum resultEnum,
        string message
        )
    {
        ResultEnum = resultEnum;
        Message = message;
    }

    public override bool Equals(object? obj)
    {
        switch (obj)
        {
            case Result result:
                return ResultEnum == result.ResultEnum;
            case ResultEnum resultE:
                return ResultEnum == resultE;
            default:
                throw new ArgumentOutOfRangeException($"Can't compare Result to {obj?.GetType()}");
        }
    }

    public static bool operator ==(Result obj1, ResultEnum obj2)
    {
        return obj1.ResultEnum == obj2;
    }

    public static bool operator !=(Result obj1, ResultEnum obj2)
    {
        return obj1.ResultEnum != obj2;
    }

    public override int GetHashCode()
    {
        throw new NotSupportedException(string.Empty);
    }
}


/// <summary>
/// Operation result with return object
/// </summary>
public readonly struct Result<T>
{
    /// <summary>
    /// Operation result enum
    /// </summary>
    public readonly ResultEnum ResultEnum { get; init; }

    /// <summary>
    /// Operation result message
    /// </summary>
    public readonly string Message { get; init; }

    /// <summary>
    /// Operation result object
    /// </summary>
    public readonly T? ResultObject { get; init; }

    /// <summary>
    /// Is operation successful
    /// </summary>
    [MemberNotNullWhen(returnValue: true, nameof(ResultObject))]
    public bool IsSuccess => ResultEnum is ResultEnum.Success;


    public Result(
        ResultEnum resultEnum,
        T? resultObj,
        string message
        )
    {
        ResultEnum = resultEnum;
        Message = message;
        ResultObject = resultObj;
    }


    public override bool Equals(object? obj)
    {
        switch (obj)
        {
            case Result<T> result:
                return ResultEnum == result.ResultEnum;
            case ResultEnum resultE:
                return ResultEnum == resultE;
            default:
                throw new ArgumentOutOfRangeException($"Can't compare Result to {obj?.GetType()}");
        }
    }

    public static bool operator ==(Result<T> obj1, ResultEnum obj2)
    {
        return obj1.ResultEnum == obj2;
    }

    public static bool operator !=(Result<T> obj1, ResultEnum obj2)
    {
        return obj1.ResultEnum != obj2;
    }

    public override int GetHashCode()
    {
        throw new NotSupportedException(string.Empty);
    }
}


public enum ResultEnum : byte
{
    /// <summary>
    /// Successful operation
    /// </summary>
    Success,
    /// <summary>
    /// Error while validating hash
    /// </summary>
    HashError,
    /// <summary>
    /// Something not found
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
    /// Task cancelled
    /// </summary>
    Cancelled,
    /// <summary>
    /// General error
    /// </summary>
    Error,
    OutOfDate
}


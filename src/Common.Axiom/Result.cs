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
        return obj switch
        {
            Result result => ResultEnum == result.ResultEnum,
            ResultEnum resultE => ResultEnum == resultE,
            _ => false
        };
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
        return ResultEnum.GetHashCode();
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
        return obj switch
        {
            Result<T> result => ResultEnum == result.ResultEnum,
            ResultEnum resultE => ResultEnum == resultE,
            _ => false
        };
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
        return ResultEnum.GetHashCode();
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


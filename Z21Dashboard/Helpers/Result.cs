using System.Text.Json.Serialization;

namespace Z21Dashboard.Helpers;

/// <summary>
/// Represents a single message, which can optionally be associated with a specific data field.
/// </summary>
/// <param name="FieldName">The name of the field the message relates to. Can be null for general messages.</param>
/// <param name="Message">The content of the message.</param>
public record ResultMessage(string? FieldName, string Message);

/// <summary>
/// Represents a collection of messages grouped under a single field name.
/// </summary>
public class ResultMessages
{
    /// <summary>
    /// The name of the field these messages apply to.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// The list of message strings for the specified field.
    /// </summary>
    public List<string> Messages { get; set; } = [];
}

public enum ResultCode
{
    Unknown,
    Ok,
    BadRequest,
}

/// <summary>
/// An abstract base class for operation results, encapsulating success status and messages.
/// </summary>
public abstract class AbstractResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Gets the list of messages associated with the result, grouped by field name.
    /// </summary>
    public List<ResultMessages> Messages { get; private set; } = [];

    /// <summary>
    /// Result code of the result object.
    /// </summary>
    public ResultCode ResultCode { get; private set; }

    protected AbstractResult(ResultCode resultCode, List<ResultMessages> messages)
    {
        ResultCode = resultCode;

        // Central success definition (Ok)
        IsSuccess = resultCode is ResultCode.Ok;

        AddListOfResultMessages(messages);
    }

    /// <summary>
    /// Adds a list of ResultMessages, grouping them by field name.
    /// </summary>
    private void AddListOfResultMessages(List<ResultMessages> messages)
    {
        foreach (var oneFieldMessage in messages)
        {
            var existing = Messages.Find(x => x.FieldName == oneFieldMessage.FieldName);

            if (existing is not null)
            {
                // Merge messages if field already exists
                existing.Messages.AddRange(oneFieldMessage.Messages);
            }
            else
            {
                Messages.Add(new ResultMessages
                {
                    FieldName = oneFieldMessage.FieldName,
                    Messages = [.. oneFieldMessage.Messages]
                });
            }
        }
    }
}

/// <summary>
/// Represents the result of an operation that does not return a value.
/// </summary>
public class Result : AbstractResult
{
    /// <summary>
    /// Private constructor to enforce use of factory methods.
    /// </summary>
    [JsonConstructor]
    private Result(ResultCode resultCode, List<ResultMessages> messages)
        : base(resultCode, messages)
    {
    }

    /// <summary>
    /// Matches the result for Success or Failure, executing the
    /// appropriate function based on the success status.
    /// </summary>
    /// <param name="onSuccess">Function to call when success.</param>
    /// <param name="onFailure">Function to call when failure.</param>
    /// <returns></returns>
    public Result Match(
        Func<Result> onSuccess,
        Func<List<ResultMessages>, Result> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Messages);
    }

    /// <summary>
    /// Creates a successful result with no messages.
    /// </summary>
    public static Result Success()
        => new(ResultCode.Ok, []);

    /// <summary>
    /// Creates a failed result with a single general message.
    /// </summary>
    public static Result Failure(string message)
        => new(ResultCode.BadRequest,
            [new() { FieldName = default, Messages = [message] }]);

    /// <summary>
    /// Creates a failed result with messages.
    /// </summary>
    public static Result Failure(List<ResultMessages> messages)
        => new(ResultCode.BadRequest,
            messages);
}

/// <summary>
/// Represents the result of an operation that returns a value of type T.
/// </summary>
public class Result<T> : AbstractResult
{
    /// <summary>
    /// The data payload of the result. It will be default (e.g., null) if the operation failed.
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// Private constructor to enforce factory usage.
    /// </summary>
    [JsonConstructor]
    private Result(ResultCode resultCode, List<ResultMessages> messages, T? data)
        : base(resultCode, messages)
    {
        Data = data;
    }

    /// <summary>
    /// Creates a successful result with a data payload.
    /// </summary>
    public static Result<T> Success(T data)
        => new(ResultCode.Ok, [], data);

    /// <summary>
    /// Creates a failed result with a message and no data.
    /// </summary>
    public static Result<T> Failure(string message)
        => new(ResultCode.BadRequest,
            [new() { FieldName = default, Messages = [message] }],
            default);

    /// <summary>
    /// Creates a failed result with a message and no data.
    /// </summary>
    public static Result<T> Failure(List<ResultMessages> messages)
        => new(ResultCode.BadRequest,
            messages,
            default);
}

/// <summary>
/// Factory class that enables type-inferred creation of Result objects.
/// </summary>
/// <remarks>
/// This allows usage like:
/// Results.Success("Hello")
/// instead of Result<string>.Success("Hello")
/// </remarks>
public static class Results
{
    // -----------------------
    // NON-GENERIC
    // -----------------------

    public static Result Success()
        => Result.Success();

    public static Result Failure(string message)
        => Result.Failure(message);

    // -----------------------
    // GENERIC (type inference enabled)
    // -----------------------

    public static Result<T> Success<T>(T data)
        => Result<T>.Success(data);

    public static Result<T> Failure<T>(string message)
        => Result<T>.Failure(message);

    public static Result<T> Failure<T>(List<ResultMessages> messages)
    => Result<T>.Failure(messages);
}
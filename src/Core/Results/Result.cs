#pragma warning disable CA1716 // 'Error' parameter name conflicts with VB.NET keyword — intentional
#pragma warning disable CA1000 // Static factory methods on generic type are intentional (Result<T>.Success/Failure pattern)
using System.Diagnostics.CodeAnalysis;

namespace Creditos.Results;

public interface IResultOf<TSelf> where TSelf : IResultOf<TSelf>
{
    static abstract TSelf Failure(Error error);
}

public class Result : IResultOf<Result>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Resultado de sucesso não pode conter erro.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Resultado de falha deve conter um erro.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    static Result IResultOf<Result>.Failure(Error error) => Failure(error);
}

public class Result<TValue> : Result, IResultOf<Result<TValue>>
{
    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    [AllowNull]
    public TValue Value => IsSuccess
        ? field!
        : throw new InvalidOperationException("O valor de um resultado de falha não pode ser acessado.");

    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);
    public static new Result<TValue> Failure(Error error) => new(default, false, error);

    static Result<TValue> IResultOf<Result<TValue>>.Failure(Error error) => Failure(error);
}
#pragma warning restore CA1000
#pragma warning restore CA1716

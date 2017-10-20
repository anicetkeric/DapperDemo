using Dapper.Model;
using System;

namespace Dapper.Repository.Repository
{
    public class SpReturnType<T> where T : class
    {
        public SpReturnType(T entity, SpTransactionMessage message)
        {
            this.entity = entity;
            Message = message;
        }

        public T entity { get; set; }
        public SpTransactionMessage Message { get; set; }
    }

    public class Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }
        public bool IsFailure => !IsSuccess;

        protected Result(bool isSuccess, string error)
        {
            if (isSuccess && error != string.Empty)
                throw new InvalidOperationException();
            if (!isSuccess && error == string.Empty)
                throw new InvalidOperationException();

            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Fail(string message)
        {
            return new Result(false, message);
        }

        public static Result<T> Fail<T>(string message)
        {
            return new Result<T>(default(T), false, message);
        }

        public static Result Ok()
        {
            return new Result(true, string.Empty);
        }

        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(value, true, string.Empty);
        }

        public static Result<T> Fail<T>(T value)
        {
            return new Result<T>(value, false, string.Empty);
        }
        public static Result<T> Fail<T>(T value, string message)
        {
            return new Result<T>(value, false, message);
        }
        public static Result Combine(params Result[] results)
        {
            foreach (Result result in results)
            {
                if (result.IsFailure)
                    return result;
            }

            return Ok();
        }
    }


    public class Result<T> : Result
    {
        private readonly T _value;

        public T Value
        {
            get
            {
                //if (!IsSuccess)
                //    throw new InvalidOperationException();

                return _value;
            }
        }

        protected internal Result(T value, bool isSuccess, string error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        public Result(bool isSuccess, string error) : base(isSuccess, error)
        {
        }
    }

    public struct Maybe<T> : IEquatable<Maybe<T>>
       where T : class
    {
        private readonly T _value;
        public T Value
        {
            get
            {
                if (HasNoValue)
                    throw new InvalidOperationException();

                return _value;
            }
        }

        public bool HasValue => _value != null;
        public bool HasNoValue => !HasValue;

        private Maybe( T value)
        {
            _value = value;
        }

        public static implicit operator Maybe<T>( T value)
        {
            return new Maybe<T>(value);
        }

        public static bool operator ==(Maybe<T> maybe, T value)
        {
            if (maybe.HasNoValue)
                return false;

            return maybe.Value.Equals(value);
        }

        public static bool operator !=(Maybe<T> maybe, T value)
        {
            return !(maybe == value);
        }

        public static bool operator ==(Maybe<T> first, Maybe<T> second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(Maybe<T> first, Maybe<T> second)
        {
            return !(first == second);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Maybe<T>))
                return false;

            var other = (Maybe<T>)obj;
            return Equals(other);
        }

        public bool Equals(Maybe<T> other)
        {
            if (HasNoValue && other.HasNoValue)
                return true;

            if (HasNoValue || other.HasNoValue)
                return false;

            return _value.Equals(other._value);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            if (HasNoValue)
                return "No value";

            return Value.ToString();
        }

        
        public T Unwrap()
        {
            if (HasValue)
                return Value;

            return default(T);
        }

        
        public K Unwrap<K>(Func<T, K> selector)
        {
            if (HasValue)
                return selector(Value);

            return default(K);
        }
    }
}
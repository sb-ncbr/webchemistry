namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An implementation of the Haskell's Maybe type.
    /// data Maybe a = Just a | Nothing
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Maybe<T>
    {        
        /// <summary>
        /// Encapsulates a value.
        /// </summary>
        public sealed class Just : Maybe<T>
        {
            /// <summary>
            /// The inner value.
            /// </summary>
            public T Value { get; private set; }
            
            /// <summary>
            /// Returns Value.ToString()
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return Value.ToString();
            }

            /// <summary>
            /// Returns Value.GetHashCode()
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            /// <summary>
            /// Checks if the other object is Just and if so, compares the inner values.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                var other = obj as Just;
                if (other != null) return other.Value.Equals(this.Value);
                return false;
            }

            /// <summary>
            /// Wrap the value into a Maybe object.
            /// </summary>
            /// <param name="value"></param>
            internal Just(T value)
            {
                if (value == null) throw new NullReferenceException("value cannot be null.");
                this.Value = value;
            }
        }
        
        /// <summary>
        /// Represents an empty result. A singleton.
        /// </summary>
        public sealed class Nothing : Maybe<T>
        {
            /// <summary>
            /// A singleton instance of the Nothing type.
            /// </summary>
            public static readonly Maybe<T> Instance = new Nothing();

            /// <summary>
            /// Checks if the other object is also Nothing.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return obj is Nothing;
            }

            /// <summary>
            /// Always returns 0.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return 0;
            }

            /// <summary>
            /// Always returns the string "Nothing".
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "Nothing";
            }

            private Nothing()
            {
            }
        }
    }

    /// <summary>
    /// Provides factory and extension methods for the generic maybe class.
    /// </summary>
    public static class Maybe
    {
        /// <summary>
        /// Creates a just value.
        /// Throws InvalidArgumentException if value = null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Maybe<T> Just<T>(T value)
        {
            return new Maybe<T>.Just(value);
        }

        /// <summary>
        /// Creates an instance of Nothing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Maybe<T> Nothing<T>()
        {
            return Maybe<T>.Nothing.Instance;
        }

        /// <summary>
        /// This is to allow the LINQ syntax "from x in m select f(x)"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="x"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static Maybe<TResult> Select<T, TResult>(this Maybe<T> x, Func<T, TResult> selector)
        {
            var v = x as Maybe<T>.Just;
            if (v == null) return Maybe.Nothing<TResult>();
            var ret = selector(v.Value);
            return ret == null ? Maybe.Nothing<TResult>() : Maybe.Just(ret);
        }

        /// <summary>
        /// This is to allow the LINQ syntax "from x in m from y in n select f(x, y)"
        /// </summary>
        /// <typeparam name="TA"></typeparam>
        /// <typeparam name="TB"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static Maybe<TResult> SelectMany<TA, TB, TResult>(this Maybe<TA> x, Func<TA, Maybe<TB>> y, Func<TA, TB, TResult> selector)
        {
            var vx = x as Maybe<TA>.Just;
            if (vx == null) return Maybe.Nothing<TResult>();

            var vy = y(vx.Value) as Maybe<TB>.Just;
            if (vy == null) return Maybe.Nothing<TResult>();

            var ret = selector(vx.Value, vy.Value);
            return ret == null ? Maybe.Nothing<TResult>() : Maybe.Just(ret);
        }

        /// <summary>
        /// Non-capturing select.
        /// </summary>
        /// <typeparam name="TA"></typeparam>
        /// <typeparam name="TB"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static Maybe<TResult> SelectMany<TA, TB, TResult>(this Maybe<TA> x, Maybe<TB> y, Func<TA, TB, TResult> selector)
        {
            var vx = x as Maybe<TA>.Just;
            if (vx == null) return Maybe.Nothing<TResult>();

            var vy = y as Maybe<TB>.Just;
            if (vy == null) return Maybe.Nothing<TResult>();

            var ret = selector(vx.Value, vy.Value);
            return ret == null ? Maybe.Nothing<TResult>() : Maybe.Just(ret);
        }

        /// <summary>
        /// If the predicate is satisfied, returs value, otherwise returns Nothing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static Maybe<T> Where<T>(this Maybe<T> value, Func<T, bool> predicate)
        {
            if (value is Maybe<T>.Nothing) return value;
            var v = value.GetValue();
            if (predicate(v)) return value;
            return Maybe.Nothing<T>();
        }

        /// <summary>
        /// Checks if the value is Nothing
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool IsNothing<T>(this Maybe<T> v)
        {
            return v is Maybe<T>.Nothing;
        }

        /// <summary>
        /// Checks if the value is Just
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool IsSomething<T>(this Maybe<T> v)
        {
            return v is Maybe<T>.Just;
        }

        /// <summary>
        /// Gets the inner value.
        /// Returns "default(T)" if Maybe is Nothing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="v"></param>
        /// <returns></returns>
        public static T GetValue<T>(this Maybe<T> v)
        {
            var t = v as Maybe<T>.Just;
            if (t != null) return t.Value;
            return default(T);
        }
        
        /// <summary>
        /// Whaps an object in the maybe type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Maybe<T> AsMaybe<T>(this T obj)
            where T : class
        {
            if (obj == null) return Maybe.Nothing<T>();
            return Maybe.Just(obj);
        }
        
        /// <summary>
        /// Convertes nullable to maybe.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Maybe<T> AsMaybe<T>(this T? obj)
            where T : struct
        {
            if (obj == null) return Maybe.Nothing<T>();
            return Maybe.Just(obj.Value);
        }

        /// <summary>
        /// Flatten "Just x"s
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IEnumerable<T> Flatten<T>(this IEnumerable<Maybe<T>> values)
        {
            foreach (var v in values)
            {
                if (v.IsSomething()) yield return v.GetValue();
            }
        }

        /// <summary>
        /// Performs an action if the value is "something".
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="action"></param>
        public static void Do<T>(this Maybe<T> value, Action<T> action)
        {
            if (value.IsSomething())
            {
                action(value.GetValue());
            }
        }

        /// <summary>
        /// Attempts to convert a string to double.
        /// Uses CultureInfo.InvariantCulture.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Maybe<double> ToDouble(this string str)
        {
            double ret;
            if (double.TryParse(str, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out ret))
            {
                return Maybe.Just(ret);
            }
            return Maybe.Nothing<double>();
        }

        /// <summary>
        /// Attempts to convert a string to int.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Maybe<int> ToInt(this string str)
        {
            int ret;
            if (int.TryParse(str, out ret))
            {
                return Maybe.Just(ret);
            }
            return Maybe.Nothing<int>();
        }
    }
}

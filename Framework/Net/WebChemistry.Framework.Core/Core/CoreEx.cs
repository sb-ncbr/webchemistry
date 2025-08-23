namespace WebChemistry.Framework.Core
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reactive.Concurrency;
    using WebChemistry.Framework.Utils;

    /// <summary>
    /// Miscelaneous extensions.
    /// </summary>
    public static class CoreEx
    {
        /// <summary>
        /// Memoizes a function using a Dictionary.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Func<T, R> Memoize<T, R>(this Func<T, R> f)
        {
            Dictionary<T, R> values = new Dictionary<T, R>();
            return new Func<T, R>(x =>
            {
                R value;
                if (values.TryGetValue(x, out value)) return value;
                value = f(x);
                values.Add(x, value);
                return value;
            });
        }
        
        /// <summary>
        /// Convert an object to a singleton IEnumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable<T> ToSingletonEnumerable<T>(this T obj)
        {
            return new T[] { obj };
        }

        /// <summary>
        /// Convert an object to a singleton array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T[] ToSingletonArray<T>(this T obj)
        {
            return new T[] { obj };
        }

        /// <summary>
        /// Returns a default value if obj is null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public static T DefaultIfNull<T>(this T obj, T def) where T : class
        {
            if (obj == null) return def;
            return obj;
        }

        /// <summary>
        /// Cast an object to a specified type. Throw if not compatible.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <returns></returns>
        public static T MustBe<T>(this object self)
            where T : class
        {
            var that = self as T;
            if (that == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Expected object {0} to have type {1}",
                        self.ToString(),
                        typeof(T).ToString()));
            }
            return that;
        }

        /// <summary>
        /// Returns a default value if obj is null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public static T DefaultIfNull<T>(this Nullable<T> obj, T def) where T : struct
        {
            if (obj == null) return def;
            return obj.Value;
        }
        
        /// <summary>
        /// Return a default value if it's not present in a dictionary.
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T DefaultIfNotPresent<S, T>(this IDictionary<S, T> dict, S key, T defaultValue = default(T))
        {
            T value;
            if (dict.TryGetValue(key, out value)) return value;
            return defaultValue;
        }
        
        /// <summary>
        /// Registers a weak listener for the PropertyChanged event.
        /// Arguments passed to callback are listener, sender and PropertyName.
        /// </summary>
        /// <typeparam name="TListener"></typeparam>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="obj"></param>
        /// <param name="listener"></param>
        /// <param name="onEventAction"></param>
        public static TSource ObservePropertyChanged<TListener, TSource>(this TSource obj, TListener listener,
            Action<TListener, TSource, string> onEventAction) 
            where TListener : class
            where TSource : INotifyPropertyChanged
        {
            var weakListener = new WeakNotifyPropertyChangedListener<TListener, TSource>(listener, obj);
            obj.PropertyChanged += weakListener.OnEvent;
            weakListener.OnEventAction = onEventAction;
            return obj;
        }
        
        /// <summary>
        /// Registers a listener notify event on IsSelected and IsHighligted properties.
        /// The interactive object is passed to the callback as the second parameter.
        /// </summary>
        /// <typeparam name="TListener"></typeparam>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="obj"></param>
        /// <param name="listener"></param>
        /// <param name="onChanged"></param>
        public static TSource ObserveInteractivePropertyChanged<TListener, TSource>(this TSource obj, TListener listener,
            Action<TListener, IInteractive> onChanged) 
            where TListener : class
            where TSource : IInteractive
        {
            var weakListener = new WeakInteractiveListener<TListener>(listener, obj);
            obj.PropertyChanged += weakListener.OnEvent;
            weakListener.OnEventAction = onChanged;
            return obj;
        }

        /// <summary>
        /// Creates an observer for an observable collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static CollectionChangedObserver<T> ObserveCollectionChanged<T>(this INotifyCollectionChanged collection, IScheduler scheduler = null)
        {
            return new CollectionChangedObserver<T>(collection, scheduler);
        }

        /// <summary>
        /// Creates an observer for an observable collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static CollectionChangedObserver<T> ObserveCollectionChanged<T>(this ObservableCollection<T> collection, IScheduler scheduler = null)
        {
            return new CollectionChangedObserver<T>(collection, scheduler);
        }

        /// <summary>
        /// Creates observable of property name from the PropertyChanged event.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IObservable<string> GetPropertyChangedObservable(this INotifyPropertyChanged obj)
        {
            return new PropertyChangedObserver(obj);
        }
        
        /// <summary>
        /// Checks if the type of xs is IList and if so, casts it to it.
        /// Otherwise calls xs.ToArray()
        /// Be careful when using this as when casted, it's the same object as apposed to LINQ's ToList().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xs"></param>
        /// <returns></returns>
        public static IList<T> AsList<T>(this IEnumerable<T> xs)
        {
            if (xs is IList<T>) return (IList<T>)xs;
            return xs.ToArray();
        }
        
        /// <summary>
        /// Join a collection to a string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xs"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        public static string JoinBy<T>(this IEnumerable<T> xs, string by = ", ")
        {
            return string.Join(by, xs);
        }

        /// <summary>
        /// Joins a specific value to a string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="xs"></param>
        /// <param name="selector"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        public static string JoinBy<T, U>(this IEnumerable<T> xs, Func<T, U> selector, string by = ", ")
        {
            return string.Join(by, xs.Select(x => selector(x)));
        }

        /// <summary>
        /// Converts an object to a JSON string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="prettyPrint"></param>
        /// <returns></returns>
        public static string ToJsonString<T>(this T obj, bool prettyPrint = true)
        {
            return JsonConvert.SerializeObject(obj, prettyPrint ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// Invariant string compare, ignores case.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool EqualIgnoreCase(this string str, string other)
        {
            return string.Compare(str, other, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        /// <summary>
        /// ordinal equal.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool EqualOrdinal(this string str, string other)
        {
            return str.Equals(other, StringComparison.Ordinal);
        }

        /// <summary>
        /// Invariant string compare, ignores case, ordinal.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool EqualOrdinalIgnoreCase(this string str, string other)
        {
            return string.Compare(str, other, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Other atom of the bond.
        /// </summary>
        /// <param name="bond"></param>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static IAtom OtherAtom(this IBond bond, IAtom atom)
        {
            if (bond.A.Id == atom.Id) return bond.B;
            return bond.A;
        }
        
        /// <summary>
        /// Clones dynamic properties from another object.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="from"></param>
        public static void ClonePropertiesFrom(this IPropertyObject obj, IPropertyObject from)
        {
            var properties = from.Properties;
            foreach (var p in properties)
            {
                if (p.Descriptor.AutoClone) obj.SetProperty(p);
            }
        }
        
        /// <summary>
        /// Sets properties.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="properties"></param>
        public static void SetProperties(this IPropertyObject obj, IEnumerable<Property> properties)
        {
            foreach (var p in properties)
            {
                obj.SetProperty(p);
            }
        }

        /// <summary>
        /// Determines whether the structure contains an atom.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static bool ContainsAtom(this IStructure structure, IAtom atom)
        {
            return structure.Atoms.GetById(atom.Id) != null;
        }
                
        /// <summary>
        /// Converts an Enumerable of T to a HashSet of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xs"></param>
        /// <returns>HashSet of the input enumerable</returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> xs, IEqualityComparer<T> comparer = null)
        {
            if (xs == null) return null;
            if (xs is IList<T>)
            {
                var list = xs as IList<T>;
                int count = list.Count;
                var ret = comparer == null ? new HashSet<T>() : new HashSet<T>(comparer);
                for (int i = 0; i < count; i++) ret.Add(list[i]);
                return ret;
            }
            if (comparer == null) return new HashSet<T>(xs);
            return new HashSet<T>(xs, comparer);
        }

        static readonly Random orderingRandom = new Random();
        /// <summary>
        /// Converts the enumerable to a randomly ordered array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xs"></param>
        /// <returns></returns>
        public static T[] ToRandomlyOrderedArray<T>(this IEnumerable<T> xs)
        {
            var randomOrder = xs.ToArray();
            for (int i = randomOrder.Length - 1; i > 0; i--)
            {
                var ind = orderingRandom.Next(i + 1);
                var tmp = randomOrder[i];
                randomOrder[i] = randomOrder[ind];
                randomOrder[ind] = tmp;
            }
            return randomOrder;
        }

        /// <summary>
        /// Randomly shuffle elements in the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xs"></param>
        public static void Shuffle<T>(this IList<T> xs)
        {
            for (int i = xs.Count - 1; i > 0; i--)
            {
                var ind = orderingRandom.Next(i + 1);
                var tmp = xs[i];
                xs[i] = xs[ind];
                xs[ind] = tmp;
            }
        }

        /// <summary>
        /// Get a random sample from an array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xs"></param>
        public static T[] RandomSample<T>(this IList<T> xs, int count)
        {
            var ret = new T[Math.Min(count, xs.Count)];
            var offset = Math.Max(xs.Count - count, 0);
            int j = 0;
            for (int i = xs.Count - 1; i >= offset; i--)
            {
                var ind = orderingRandom.Next(i + 1);
                var tmp = xs[i];
                xs[i] = xs[ind];
                xs[ind] = tmp;
                ret[j++] = xs[i];
            }
            return ret;
        }

        /// <summary>
        /// Created an read-only collection from the enumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xs"></param>
        /// <returns>ReadOnlyCollection of the input enumerable.</returns>
        public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> xs)
        {
            if (xs is IList<T>) return new ReadOnlyCollection<T>((IList<T>)xs);
            return new ReadOnlyCollection<T>(xs.ToArray());
        }

        /// <summary>
        /// Converts double to string using invariant culture.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToStringInvariant(this double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts double to string using invariant culture and specified format.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToStringInvariant(this double value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
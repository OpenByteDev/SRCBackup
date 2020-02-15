using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRCBackup {
    public static class Extensions {

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) =>
            dictionary.GetOrDefault(key, default(TValue));

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) =>
            dictionary != null && dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValueSupplier) =>
            dictionary != null && dictionary.TryGetValue(key, out TValue value) ? value : defaultValueSupplier();

        public static TValue? GetOrNull<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : struct =>
            dictionary != null && dictionary.TryGetValue(key, out TValue value) ? (TValue?)value : null;

    }
}

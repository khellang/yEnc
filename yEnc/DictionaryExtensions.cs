using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace yEnc
{
    internal static class DictionaryExtensions
    {
        public static T GetAndConvert<T>([NotNull] this IDictionary<string, string> dictionary, [NotNull] string key, [NotNull] Func<string, T> converter)
        {
            Check.NotNull(dictionary, "dictionary");
            Check.NotEmpty(key, "key");
            Check.NotNull(converter, "converter");

            string stringValue;
            return dictionary.TryGetValue(key, out stringValue) ? converter.Invoke(stringValue) : default(T);
        }

        public static T GetOrDefault<T>([NotNull] this IDictionary<string, T> dictionary, [NotNull] string key)
        {
            Check.NotNull(dictionary, "dictionary");
            Check.NotEmpty(key, "key");

            T value;
            return dictionary.TryGetValue(key, out value) ? value : default(T);
        }
    }
}
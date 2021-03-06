﻿// Mr Oleksandr Duzhar licenses this file to you under the MIT license.
// If you need the License file, please send an email to duzhar@googlemail.com
// 
namespace Il2Native.Logic.Project
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class ProjectProperties : IDictionary<string, string>
    {
        readonly IDictionary<string, string> dict;
        readonly IDictionary<string, string> lowerDict;

        public ProjectProperties()
        {
            this.dict = new Dictionary<string, string>();
            this.lowerDict = new Dictionary<string, string>();
        }

        public ProjectProperties(IDictionary<string, string> dict)
        {
            this.dict = dict;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return this.dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Add(KeyValuePair<string, string> item)
        {
            this.dict.Add(item);
        }

        public void Clear()
        {
            this.dict.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return this.dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            this.dict.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return this.dict.Remove(item);
        }

        public int Count
        {
            get { return this.dict.Count; }
        }

        public bool IsReadOnly
        {
            get { return this.dict.IsReadOnly; }
        }

        public bool ContainsKey(string key)
        {
            return this.dict.ContainsKey(key);
        }

        public void Add(string key, string value)
        {
            this.dict.Add(key, value);
        }

        public bool Remove(string key)
        {
            return this.dict.Remove(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return this.dict.TryGetValue(key, out value);
        }

        public string this[string key]
        {
            get
            {
                var lowerKey = key.ToLowerInvariant();
                var hasKey = this.dict.ContainsKey(key);
                var hasLowerKey = this.lowerDict.ContainsKey(lowerKey);
                if (!hasKey && !hasLowerKey)
                {
                    var value = Environment.GetEnvironmentVariable(key);
                    return value;
                }

                return hasKey ? this.dict[key] : hasLowerKey ? this.lowerDict[lowerKey] : null;
            }

            set { this.dict[key] = value; this.lowerDict[key.ToLowerInvariant()] = value; }
        }

        public ICollection<string> Keys 
        {
            get { return this.dict.Keys; }
        }

        public ICollection<string> Values
        {
            get { return this.dict.Values; }
        }
    }
}

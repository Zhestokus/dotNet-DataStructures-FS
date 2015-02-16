using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace DataStructuresFsConsoleApp.RWaySe
{
    public class RWayTrieSeBsDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly RWayTrieSeBs<TKey, TValue> rway;

        public RWayTrieSeBsDictionary(Stream stream, bool open)
            : this(stream, new BinaryFormatter(), new BinaryFormatter(), open)
        {
        }

        public RWayTrieSeBsDictionary(Stream stream, IFormatter keySerializer, IFormatter valueSerializer, bool open)
        {
            rway = new RWayTrieSeBs<TKey, TValue>(stream, keySerializer, valueSerializer, open);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var pair in rway)
            {
                yield return pair;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            rway.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            rway.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            var node = rway.Search(item.Key);
            return (node != null);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var pair in rway)
            {
                if (arrayIndex >= array.Length)
                {
                    return;
                }

                array[arrayIndex++] = pair;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return rway.Remove(item.Key);
        }

        public int Count
        {
            get { return rway.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool ContainsKey(TKey key)
        {
            var node = rway.Search(key);
            return (node != null);
        }

        public void Add(TKey key, TValue value)
        {
            rway.Add(key, value);
        }

        public bool Remove(TKey key)
        {
            return rway.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);

            var node = rway.Search(key);
            if (node == null)
            {
                return false;
            }

            value = node.Value;
            return true;
        }

        public TValue this[TKey key]
        {
            get
            {
                var node = rway.Search(key);
                if (node == null)
                {
                    throw new Exception("Key not found");
                }

                return node.Value;
            }
            set
            {
                rway.AddOrUpdate(key, value);
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                var list = new List<TKey>();

                foreach (var pair in rway)
                {
                    list.Add(pair.Key);
                }

                return list;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                var list = new List<TValue>();

                foreach (var pair in rway)
                {
                    list.Add(pair.Value);
                }

                return list;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace DataStructuresFsConsoleApp.RWay
{
    public class RWayTrieBs<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Stream _stream;

        private readonly IFormatter _keySerializer;
        private readonly IFormatter _valueSerializer;

        private readonly RWayNodeBs<TKey, TValue> _root;

        private int _count;
        private long _rootPosition;

        public RWayTrieBs(Stream stream, bool open)
            : this(stream, new BinaryFormatter(), new BinaryFormatter(), open)
        {
        }

        public RWayTrieBs(Stream stream, IFormatter keySerializer, IFormatter valueSerializer, bool open)
        {
            _stream = stream;

            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;

            _stream.Seek(0L, SeekOrigin.Begin);

            if (open)
            {
                var reader = new BinaryReader(_stream);

                _count = reader.ReadInt32();
                _rootPosition = reader.ReadInt64();

                _root = new RWayNodeBs<TKey, TValue>(_rootPosition, stream, keySerializer, valueSerializer);
            }
            else
            {
                var writer = new BinaryWriter(_stream);
                writer.Write(_count);
                writer.Write(_rootPosition);

                _root = new RWayNodeBs<TKey, TValue>(_stream, false, default(TKey), default(TValue), _keySerializer, _valueSerializer);
            }
        }

        public int Count
        {
            get { return _count; }
        }

        public void Add(TKey key, TValue value)
        {
            var keyBytes = SerializeKey(key);
            Insert(key, value, keyBytes, false);
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            var keyBytes = SerializeKey(key);
            Insert(key, value, keyBytes, true);
        }

        private void Insert(TKey key, TValue value, byte[] keyBytes, bool update)
        {
            var node = _root;

            for (int i = 0; i < keyBytes.Length; i++)
            {
                var @byte = keyBytes[i];

                var n = node[@byte];
                if (n == null)
                {
                    n = new RWayNodeBs<TKey, TValue>(_stream, false, default(TKey), default(TValue), _keySerializer, _valueSerializer);
                    node[@byte] = n;
                }

                node = n;
            }

            if (!node.Leaf)
            {
                node.Leaf = true;
                node.Key = key;
                node.Value = value;

                _count++;

                return;
            }

            if (update)
            {
                node.Key = key;
                node.Value = value;

                return;
            }


            throw new Exception("Key already exists");
        }

        public bool Remove(TKey key)
        {
            return Remove(_root, key);
        }
        private bool Remove(RWayNodeBs<TKey, TValue> node, TKey key)
        {
            var n = Search(node, key);
            if (n == null || !n.Leaf)
                return false;

            n.Leaf = false;
            _count--;

            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < RWayNodeBs<TKey, TValue>.Size; i++)
            {
                _root[i] = null;
            }

            _count = 0;
        }

        public RWayNodeBs<TKey, TValue> Search(TKey key)
        {
            return Search(_root, key);
        }
        private RWayNodeBs<TKey, TValue> Search(RWayNodeBs<TKey, TValue> node, TKey key)
        {
            var keyBytes = SerializeKey(key);
            return Search(node, keyBytes);
        }
        private RWayNodeBs<TKey, TValue> Search(RWayNodeBs<TKey, TValue> node, byte[] keyBytes)
        {
            if (node == null)
            {
                return null;
            }

            for (int i = 0; i < keyBytes.Length; i++)
            {
                var @byte = keyBytes[i];
                node = node[@byte];

                if (node == null)
                {
                    return null;
                }
            }

            if (!node.Leaf)
                return null;

            return node;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Travers(_root);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerator<KeyValuePair<TKey, TValue>> Travers(RWayNodeBs<TKey, TValue> node)
        {
            var stack = new Stack<RWayNodeBs<TKey, TValue>>();
            stack.Push(node);

            while (stack.Count > 0)
            {
                var n = stack.Pop();

                foreach (var child in n)
                {
                    if (child != null)
                        stack.Push(child);
                }

                if (n.Leaf)
                {
                    yield return new KeyValuePair<TKey, TValue>(n.Key, n.Value);
                }
            }
        }

        public void Flush()
        {
            const int start = sizeof(int) + sizeof(long);
            _stream.Seek(start, SeekOrigin.Begin);

            _root.Flush();
            _rootPosition = _root.Position;

            _stream.Seek(0L, SeekOrigin.Begin);

            var writer = new BinaryWriter(_stream);
            writer.Write(_count);
            writer.Write(_rootPosition);
        }

        private byte[] SerializeKey(TKey key)
        {
            using (var stream = new MemoryStream())
            {
                _keySerializer.Serialize(stream, key);

                return stream.ToArray();
            }
        }

        private TKey DeserializeKey(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return (TKey)_keySerializer.Deserialize(stream);
            }
        }

        private byte[] SerializeValue(TValue value)
        {
            using (var stream = new MemoryStream())
            {
                _valueSerializer.Serialize(stream, value);

                return stream.ToArray();
            }
        }

        private TValue DeserializeValue(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return (TValue)_valueSerializer.Deserialize(stream);
            }
        }
    }
}

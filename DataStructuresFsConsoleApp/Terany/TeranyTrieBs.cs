using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace DataStructuresFsConsoleApp.Terany
{
    public class TeranyTrieBs<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Stream _stream;

        private readonly IFormatter _keySerializer;
        private readonly IFormatter _valueSerializer;

        private int _count;
        private long _rootPosition;
        private TeranyNodeBs<TKey, TValue> _root;

        public TeranyTrieBs(Stream stream, bool open)
            : this(stream, new BinaryFormatter(), new BinaryFormatter(), open)
        {
        }

        public TeranyTrieBs(Stream stream, IFormatter keySerializer, IFormatter valueSerializer, bool open)
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

                _root = new TeranyNodeBs<TKey, TValue>(_rootPosition, stream, keySerializer, valueSerializer);
            }
            else
            {
                var writer = new BinaryWriter(_stream);
                writer.Write(_count);
                writer.Write(_rootPosition);
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
            var index = 0;
            var length = keyBytes.Length - 1;

            if (_root == null)
                _root = new TeranyNodeBs<TKey, TValue>(_stream, keyBytes[0], _keySerializer, _valueSerializer);

            var node = _root;

            while (true)
            {
                var @byte = keyBytes[index];

                if (@byte < node.Byte)
                {
                    if (node.Left == null)
                        node.Left = new TeranyNodeBs<TKey, TValue>(_stream, @byte, _keySerializer, _valueSerializer);

                    node = node.Left;
                }
                else if (@byte > node.Byte)
                {
                    if (node.Right == null)
                        node.Right = new TeranyNodeBs<TKey, TValue>(_stream, @byte, _keySerializer, _valueSerializer);

                    node = node.Right;
                }
                else if (index < length)
                {
                    if (node.Middle == null)
                        node.Middle = new TeranyNodeBs<TKey, TValue>(-1L, _stream, keyBytes[index + 1], _keySerializer, _valueSerializer);

                    node = node.Middle;
                    index++;
                }
                else
                {
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
            }
        }

        public bool Remove(TKey key)
        {
            var node = Search(key);
            if (node != null && node.Leaf)
            {
                node.Leaf = false;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _root = null;
            _count = 0;
        }

        public TeranyNodeBs<TKey, TValue> Search(TKey key)
        {
            return Search(_root, key);
        }
        private TeranyNodeBs<TKey, TValue> Search(TeranyNodeBs<TKey, TValue> node, TKey key)
        {
            var keyBytes = SerializeKey(key);
            return Search(node, keyBytes);
        }
        private TeranyNodeBs<TKey, TValue> Search(TeranyNodeBs<TKey, TValue> node, byte[] keyBytes)
        {
            if (node == null)
            {
                return null;
            }

            var index = 0;
            var length = keyBytes.Length - 1;

            while (true)
            {
                if (node == null)
                    return null;

                var @byte = keyBytes[index];

                if (@byte < node.Byte)
                    node = node.Left;
                else if (@byte > node.Byte)
                    node = node.Right;
                else if (index < length)
                {
                    node = node.Middle;
                    index++;
                }
                else
                {
                    if (!node.Leaf)
                        return null;

                    return node;
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Travers(_root);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerator<KeyValuePair<TKey, TValue>> Travers(TeranyNodeBs<TKey, TValue> node)
        {
            var stack = new Stack<TeranyNodeBs<TKey, TValue>>();
            stack.Push(node);

            while (stack.Count > 0)
            {
                var n = stack.Pop();

                if (n.Left != null)
                    stack.Push(n.Left);

                if (n.Middle != null)
                    stack.Push(n.Middle);

                if (n.Right != null)
                    stack.Push(n.Right);

                if (n.Leaf)
                {
                    yield return new KeyValuePair<TKey, TValue>(n.Key, n.Value);
                }
            }
        }

        public void Flush()
        {
            const int start = sizeof (int) + sizeof (long);
            _stream.Seek(start, SeekOrigin.Begin);

            if (_root != null)
            {
                _root.Flush();
                _rootPosition = _root.Position;
            }

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
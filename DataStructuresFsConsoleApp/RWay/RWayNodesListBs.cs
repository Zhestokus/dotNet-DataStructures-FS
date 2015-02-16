using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using DataStructuresFsConsoleApp.Common;

namespace DataStructuresFsConsoleApp.RWay
{
    public class RWayNodesListBs<TKey, TValue> : IEnumerable<RWayNodeBs<TKey, TValue>>
    {
        private const int Size = 256;
        private readonly Stream _stream;

        private readonly IFormatter _keySerializer;
        private readonly IFormatter _valueSerializer;

        private long _position;

        private long[] _childPositions;
        private RWayNodeBs<TKey, TValue>[] _children;

        private bool _inited;
        private bool _changed;

        public RWayNodesListBs(long position, Stream stream, IFormatter keySerializer, IFormatter valueSerializer)
        {
            _position = position;
            _stream = stream;

            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;
        }

        public long Position
        {
            get { return _position; }
        }

        public RWayNodeBs<TKey, TValue> this[int index]
        {
            get { return ReadNode(index); }
            set { WriteNode(value, index); }
        }

        public IEnumerator<RWayNodeBs<TKey, TValue>> GetEnumerator()
        {
            for (int i = 0; i < Size; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void Init()
        {
            if (_inited)
                return;

            _inited = true;

            _childPositions = new long[Size];
            _children = new RWayNodeBs<TKey, TValue>[Size];

            if (_position == -1L)
            {
                for (int i = 0; i < Size; i++)
                    _childPositions[i] = -1L;

                _changed = true;
            }
            else
            {
                var seek = _position - _stream.Position;
                if (seek != 0L)
                    _stream.Seek(seek, SeekOrigin.Current);

                var reader = new BinaryReader(_stream);

                var bytes = reader.ReadBytes(sizeof(long) * Size);

                for (int i = 0; i < Size; i++)
                    _childPositions[i] = BufferUtil.ReadLong(bytes, i * 8);

                //for (int i = 0; i < Size; i++)
                //    _childPositions[i] = reader.ReadInt64();
            }
        }

        private RWayNodeBs<TKey, TValue> ReadNode(int index)
        {
            Init();

            if (_children == null)
                return null;

            var child = _children[index];
            var position = _childPositions[index];

            if (child == null && position > -1)
            {
                child = new RWayNodeBs<TKey, TValue>(position, _stream, _keySerializer, _valueSerializer);
                _children[index] = child;
            }

            return child;
        }

        private void WriteNode(RWayNodeBs<TKey, TValue> node, int index)
        {
            Init();

            _children[index] = node;
            _childPositions[index] = (node == null ? -1L : node.Position);

            _changed = true;
        }

        public void Flush()
        {
            Init();

            for (int i = 0; i < Size; i++)
            {
                var child = _children[i];
                if (child != null)
                {
                    child.Flush();
                    _childPositions[i] = child.Position;
                }
            }

            if (_changed)
            {
                if (_position == -1L)
                {
                    _position = _stream.Seek(0L, SeekOrigin.End);
                }
                else
                {
                    var seek = _position - _stream.Position;
                    if (seek != 0L)
                        _stream.Seek(seek, SeekOrigin.Current);
                }

                var bytes = new byte[sizeof(long) * Size];

                for (int i = 0; i < Size; i++)
                    BufferUtil.Write(bytes, i * 8, _childPositions[i]);

                _stream.Write(bytes, 0, bytes.Length);

                _changed = false;
                //var writer = new BinaryWriter(_stream);

                //for (int i = 0; i < Size; i++)
                //    writer.Write(_childPositions[i]);
            }
        }
    }
}

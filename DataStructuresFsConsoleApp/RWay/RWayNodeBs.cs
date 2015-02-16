using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using DataStructuresFsConsoleApp.Common;

namespace DataStructuresFsConsoleApp.RWay
{
    public class RWayNodeBs<TKey, TValue> : IEnumerable<RWayNodeBs<TKey, TValue>>
    {
        public const int Size = 256;

        private readonly Stream _stream;

        private readonly LasyLoader<TKey> _keyLoader;
        private readonly LasyLoader<TValue> _valueLoader;

        private readonly RWayNodesListBs<TKey, TValue> _nodesLoader;

        private bool _leaf;
        private bool _changed;
        private long _position;

        public RWayNodeBs(long position, Stream stream, IFormatter keySerializer, IFormatter valueSerializer)
        {
            _position = position;

            _stream = stream;

            var reader = new BinaryReader(stream);

            var seek = position - stream.Position;
            if (seek != 0L)
                _stream.Seek(seek, SeekOrigin.Current);

            var bytes = reader.ReadBytes(25);
            _leaf = BufferUtil.ReadBool(bytes, 0);

            var nodesPosition = BufferUtil.ReadLong(bytes, 1);
            var keyPosition = BufferUtil.ReadLong(bytes, 9);
            var valuePosition = BufferUtil.ReadLong(bytes, 17);

            //_leaf = reader.ReadBoolean();

            //var nodesPosition = reader.ReadInt64();
            //var keyPosition = reader.ReadInt64();
            //var valuePosition = reader.ReadInt64();

            _nodesLoader = new RWayNodesListBs<TKey, TValue>(nodesPosition, _stream, keySerializer, valueSerializer);
            _keyLoader = new LasyLoader<TKey>(keyPosition, stream, keySerializer);
            _valueLoader = new LasyLoader<TValue>(valuePosition, stream, valueSerializer);
        }

        public RWayNodeBs(Stream stream, bool leaf, TKey key, TValue value, IFormatter keySerializer, IFormatter valueSerializer)
        {
            _position = -1L;
            _stream = stream;
            _leaf = leaf;

            _changed = true;

            const long nodesPosition = -1L;
            const long keyPosition = -1L;
            const long valuePosition = -1L;

            _nodesLoader = new RWayNodesListBs<TKey, TValue>(nodesPosition, _stream, keySerializer, valueSerializer);

            _keyLoader = new LasyLoader<TKey>(keyPosition, stream, keySerializer, key);
            _valueLoader = new LasyLoader<TValue>(valuePosition, stream, valueSerializer, value);
        }

        public long Position
        {
            get { return _position; }
        }

        public bool Leaf
        {
            get { return _leaf; }
            set
            {
                _leaf = value;
                _changed = true;
            }
        }

        public TKey Key
        {
            get { return _keyLoader.LoadObject(); }
            set
            {
                _keyLoader.WriteObject(value);
                _changed = true;
            }
        }

        public byte[] KeyBytes
        {
            get { return _keyLoader.LoadBytes(); }
        }

        public TValue Value
        {
            get { return _valueLoader.LoadObject(); }
            set
            {
                _valueLoader.WriteObject(value);
                _changed = true;
            }
        }

        public byte[] ValueBytes
        {
            get { return _valueLoader.LoadBytes(); }
        }

        public RWayNodeBs<TKey, TValue> this[int index]
        {
            get { return _nodesLoader[index]; }
            set
            {
                _nodesLoader[index] = value;
                _changed = true;
            }
        }

        public void Flush()
        {
            _keyLoader.Flush();
            _valueLoader.Flush();
            _nodesLoader.Flush();

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

                var bytes = new byte[25];

                BufferUtil.Write(bytes, 0, _leaf);
                BufferUtil.Write(bytes, 1, _nodesLoader.Position);
                BufferUtil.Write(bytes, 9, _keyLoader.Position);
                BufferUtil.Write(bytes, 17, _valueLoader.Position);

                _stream.Write(bytes, 0, bytes.Length);

                _changed = false;

                //var writer = new BinaryWriter(_stream);

                //writer.Write(_leaf);
                //writer.Write(_nodesLoader.Position);
                //writer.Write(_keyLoader.Position);
                //writer.Write(_valueLoader.Position);
            }
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
    }
}

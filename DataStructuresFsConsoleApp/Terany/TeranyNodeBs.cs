using System.IO;
using System.Runtime.Serialization;
using DataStructuresFsConsoleApp.Common;

namespace DataStructuresFsConsoleApp.Terany
{
    public class TeranyNodeBs<TKey, TValue>
    {
        private readonly Stream _stream;

        private readonly LasyLoader<TKey> _keyLoader;
        private readonly LasyLoader<TValue> _valueLoader;

        private readonly IFormatter _keySerializer;
        private readonly IFormatter _valueSerializer;

        private long _leftPosition;
        private long _middlePosition;
        private long _rightPosition;

        private byte _byte;
        private bool _leaf;
        private bool _changed;
        private long _position;

        private TeranyNodeBs<TKey, TValue> _left;
        private TeranyNodeBs<TKey, TValue> _middle;
        private TeranyNodeBs<TKey, TValue> _right;

        public TeranyNodeBs(long position, Stream stream, IFormatter keySerializer, IFormatter valueSerializer)
        {
            _position = position;

            _stream = stream;

            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;

            var reader = new BinaryReader(stream);

            var seek = position - stream.Position;
            if (seek != 0L)
                stream.Seek(seek, SeekOrigin.Current);

            var bytes = reader.ReadBytes(42);

            _byte = BufferUtil.ReadByte(bytes, 0);
            _leaf = BufferUtil.ReadBool(bytes, 1);

            _leftPosition = BufferUtil.ReadLong(bytes, 2);
            _middlePosition = BufferUtil.ReadLong(bytes, 10);
            _rightPosition = BufferUtil.ReadLong(bytes, 18);

            var keyPosition = BufferUtil.ReadLong(bytes, 26);
            var valuePosition = BufferUtil.ReadLong(bytes, 34);

            _keyLoader = new LasyLoader<TKey>(keyPosition, stream, keySerializer);
            _valueLoader = new LasyLoader<TValue>(valuePosition, stream, valueSerializer);
        }

        public TeranyNodeBs(Stream stream, byte @byte, IFormatter keySerializer, IFormatter valueSerializer)
            : this(-1L, stream, @byte, false, default(TKey), default(TValue), keySerializer, valueSerializer)
        {
        }

        public TeranyNodeBs(long position, Stream stream, byte @byte, IFormatter keySerializer, IFormatter valueSerializer)
            : this(position, stream, @byte, false, default(TKey), default(TValue), keySerializer, valueSerializer)
        {
        }

        public TeranyNodeBs(long position, Stream stream, byte @byte, bool leaf, TKey key, TValue value, IFormatter keySerializer, IFormatter valueSerializer)
        {
            _position = position;

            _stream = stream;

            _byte = @byte;
            _leaf = leaf;
            _changed = true;

            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;

            _leftPosition = -1L;
            _middlePosition = -1L;
            _rightPosition = -1L;

            const long keyPosition = -1L;
            const long valuePosition = -1L;

            _keyLoader = new LasyLoader<TKey>(keyPosition, _stream, keySerializer, key);
            _valueLoader = new LasyLoader<TValue>(valuePosition, _stream, valueSerializer, value);
        }

        public long Position
        {
            get { return _position; }
        }

        public byte Byte
        {
            get { return _byte; }
            set
            {
                _byte = value;
                _changed = true;
            }
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
            get
            {
                return _valueLoader.LoadBytes();
            }
        }

        public TeranyNodeBs<TKey, TValue> Left
        {
            get
            {
                if (_left == null && _leftPosition > -1)
                {
                    _left = new TeranyNodeBs<TKey, TValue>(_leftPosition, _stream, _keySerializer, _valueSerializer);
                }

                return _left;
            }
            set
            {
                //TODO: Performace Critical
                _left = value;
                _leftPosition = (_left == null ? -1 : _left.Position);

                _changed = true;
            }
        }

        public TeranyNodeBs<TKey, TValue> Middle
        {
            get
            {
                if (_middle == null && _middlePosition > -1)
                {
                    _middle = new TeranyNodeBs<TKey, TValue>(_middlePosition, _stream, _keySerializer, _valueSerializer);
                }

                return _middle;
            }
            set
            {
                //TODO: Performace Critical
                _middle = value;
                _middlePosition = (_middle == null ? -1 : _middle.Position);

                _changed = true;
            }
        }

        public TeranyNodeBs<TKey, TValue> Right
        {
            get
            {
                if (_right == null && _rightPosition > -1)
                {
                    _right = new TeranyNodeBs<TKey, TValue>(_rightPosition, _stream, _keySerializer, _valueSerializer);
                }

                return _right;
            }
            set
            {
                //TODO: Performace Critical
                _right = value;
                _rightPosition = (_right == null ? -1 : _right.Position);

                _changed = true;
            }
        }

        public void Flush()
        {
            _keyLoader.Flush();
            _valueLoader.Flush();

            if (_left != null)
            {
                _left.Flush();
                _leftPosition = _left.Position;
            }

            if (_middle != null)
            {
                _middle.Flush();
                _middlePosition = _middle.Position;
            }

            if (_right != null)
            {
                _right.Flush();
                _rightPosition = _right.Position;
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

                var bytes = new byte[42];

                BufferUtil.Write(bytes, 0, _byte);
                BufferUtil.Write(bytes, 1, _leaf);

                BufferUtil.Write(bytes, 2, _leftPosition);
                BufferUtil.Write(bytes, 10, _middlePosition);
                BufferUtil.Write(bytes, 18, _rightPosition);

                BufferUtil.Write(bytes, 26, _keyLoader.Position);
                BufferUtil.Write(bytes, 34, _valueLoader.Position);

                _stream.Write(bytes, 0, bytes.Length);

                _changed = false;
            }
        }
    }
}

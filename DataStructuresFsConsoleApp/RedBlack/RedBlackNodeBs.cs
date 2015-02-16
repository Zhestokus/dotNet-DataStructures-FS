using System.IO;
using System.Runtime.Serialization;
using DataStructuresFsConsoleApp.Common;

namespace DataStructuresFsConsoleApp.RedBlack
{
    public class RedBlackNodeBs<TKey, TValue>
    {
        public const bool RED = true;
        public const bool BLACK = false;

        private readonly Stream _stream;

        private readonly IFormatter _keySerializer;
        private readonly IFormatter _valueSerializer;

        private readonly LasyLoader<TKey> _keyLoader;
        private readonly LasyLoader<TValue> _valueLoader;

        private int _count;
        private bool _color;
        private bool _changed;
        private long _position;

        private long _leftPosition;
        private long _rightPosition;

        private RedBlackNodeBs<TKey, TValue> _left;
        private RedBlackNodeBs<TKey, TValue> _right;

        public RedBlackNodeBs(long position, Stream stream, IFormatter keySerializer, IFormatter valueSerializer)
        {
            _position = position;

            _stream = stream;

            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;

            var reader = new BinaryReader(stream);

            var seek = position - stream.Position;
            if (seek != 0L)
                stream.Seek(seek, SeekOrigin.Current);

            var bytes = reader.ReadBytes(37);

            _color = BufferUtil.ReadBool(bytes, 0);
            _count = BufferUtil.ReadInt(bytes, 1);

            _leftPosition = BufferUtil.ReadLong(bytes, 5);
            _rightPosition = BufferUtil.ReadLong(bytes, 13);

            var keyPosition = BufferUtil.ReadLong(bytes, 21);
            var valuePosition = BufferUtil.ReadLong(bytes, 29);

            _keyLoader = new LasyLoader<TKey>(keyPosition, stream, keySerializer);
            _valueLoader = new LasyLoader<TValue>(valuePosition, stream, valueSerializer);
        }

        public RedBlackNodeBs(Stream stream, TKey key, TValue value, IFormatter keySerializer, IFormatter valueSerializer)
            : this(stream, key, value, BLACK, keySerializer, valueSerializer)
        {
        }
        public RedBlackNodeBs(Stream stream, TKey key, TValue value, bool color, IFormatter keySerializer, IFormatter valueSerializer)
            : this(-1L, stream, key, value, color, 1, keySerializer, valueSerializer)
        {
        }
        public RedBlackNodeBs(long position, Stream stream, TKey key, TValue value, IFormatter keySerializer, IFormatter valueSerializer)
            : this(position, stream, key, value, BLACK, 1, keySerializer, valueSerializer)
        {
        }
        public RedBlackNodeBs(Stream stream, TKey key, TValue value, bool color, int count, IFormatter keySerializer, IFormatter valueSerializer)
            : this(-1L, stream, key, value, color, count, keySerializer, valueSerializer)
        {
        }

        public RedBlackNodeBs(long position, Stream stream, TKey key, TValue value, bool color, int count, IFormatter keySerializer, IFormatter valueSerializer)
        {
            _position = position;

            _stream = stream;

            _color = color;
            _count = count;
            _changed = true;

            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;

            _leftPosition = -1L;
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

        public bool Color
        {
            get { return _color; }
            set
            {
                _color = value;
                _changed = true;
            }
        }

        public int Count
        {
            get { return _count; }
            set
            {
                _count = value;
                _changed = true;
            }
        }

        public RedBlackNodeBs<TKey, TValue> Left
        {
            get
            {
                if (_left == null && _leftPosition > -1)
                {
                    _left = new RedBlackNodeBs<TKey, TValue>(_leftPosition, _stream, _keySerializer, _valueSerializer);
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

        public RedBlackNodeBs<TKey, TValue> Right
        {
            get
            {
                if (_right == null && _rightPosition > -1)
                {
                    _right = new RedBlackNodeBs<TKey, TValue>(_rightPosition, _stream, _keySerializer, _valueSerializer);
                }

                return _right;
            }
            set
            {
                //TODO: Performace Critical
                _right = value;
                _rightPosition = (_left == null ? -1 : _right.Position);

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

                var bytes = new byte[37];

                BufferUtil.Write(bytes, 0, _color);
                BufferUtil.Write(bytes, 1, _count);
                BufferUtil.Write(bytes, 5, _leftPosition);
                BufferUtil.Write(bytes, 13, _rightPosition);

                BufferUtil.Write(bytes, 21, _keyLoader.Position);
                BufferUtil.Write(bytes, 29, _valueLoader.Position);

                _stream.Write(bytes, 0, bytes.Length);

                _changed = false;
            }
        }
    }
}
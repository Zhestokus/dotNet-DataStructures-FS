using System.IO;
using System.Runtime.Serialization;

namespace DataStructuresFsConsoleApp.Common
{
    public class LasyLoader<TObject>
    {
        private readonly IFormatter _serializer;

        private readonly BinaryReader _reader;
        private readonly Stream _stream;

        private long _position;

        private bool _bytesLoaded;
        private bool _objectLoaded;

        private byte[] _bytes;
        private TObject _object;

        private bool _changed;

        public LasyLoader(long position, Stream stream, IFormatter serializer)
        {
            _stream = stream;
            _position = position;
            _serializer = serializer;

            _reader = new BinaryReader(_stream);
        }

        public LasyLoader(long position, Stream stream, IFormatter serializer, TObject obj)
        {
            _stream = stream;
            _position = position;
            _serializer = serializer;

            _object = obj;
            _bytes = null;

            _bytesLoaded = true;
            _objectLoaded = true;

            _changed = (_position == -1L);
            _reader = new BinaryReader(_stream);

            _bytes = GetObjBytes(obj);
        }

        public LasyLoader(long position, Stream stream, IFormatter serializer, TObject obj, byte[] bytes)
        {
            _stream = stream;
            _position = position;
            _serializer = serializer;

            _object = obj;
            _bytes = bytes;

            _bytesLoaded = true;
            _objectLoaded = true;

            _changed = (_position == -1L);
            _reader = new BinaryReader(_stream);
        }

        public long Position
        {
            get { return _position; }
        }

        public byte[] LoadBytes()
        {
            if (!_bytesLoaded)
            {
                if (_position > -1)
                {
                    var seek = _position - _stream.Position;
                    if (seek != 0L)
                        _stream.Seek(seek, SeekOrigin.Current);

                    var bytesLen = _reader.ReadInt32();
                    _bytes = _reader.ReadBytes(bytesLen);
                }

                _bytesLoaded = true;
            }

            return _bytes;
        }

        public TObject LoadObject()
        {
            if (!_objectLoaded)
            {
                var objBytes = LoadBytes();
                if (objBytes != null)
                {
                    _object = Deserialize(objBytes);
                }

                _objectLoaded = true;
            }

            return _object;
        }

        public void WriteObject(TObject obj)
        {
            var objBytes = GetObjBytes(obj);

            if (_objectLoaded && _bytesLoaded)
            {
                if (ReferenceEquals(_object, obj) || BufferUtil.ByteArrayEquals(_bytes, objBytes))
                {
                    return;
                }
            }

            _object = obj;
            _bytes = objBytes;
            _position = -1L;

            _bytesLoaded = true;
            _objectLoaded = true;

            _changed = true;
        }

        public void Flush()
        {
            if (_changed)
            {
                if (_bytes != null && _bytes.Length > 0)
                {
                    _position = _stream.Seek(0L, SeekOrigin.End);

                    var writer = new BinaryWriter(_stream);

                    writer.Write(_bytes.Length);
                    writer.Write(_bytes);
                }

                _changed = false;
            }
        }

        private byte[] GetObjBytes(TObject obj)
        {
            if (!ReferenceEquals(obj, null))
            {
                return Serialize(obj);
            }

            return null;
        }

        private byte[] Serialize(TObject @object)
        {
            using (var stream = new MemoryStream())
            {
                _serializer.Serialize(stream, @object);

                return stream.ToArray();
            }
        }

        private TObject Deserialize(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return (TObject)_serializer.Deserialize(stream);
            }
        }
    }
}

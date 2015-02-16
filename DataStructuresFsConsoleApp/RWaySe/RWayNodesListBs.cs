using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using DataStructuresFsConsoleApp.Common;

namespace DataStructuresFsConsoleApp.RWaySe
{
    public class RWayNodesListBs<TKey, TValue> : IEnumerable<RWayNodeBs<TKey, TValue>>
    {
        private const int Size = 16;

        private readonly Stream _stream;

        private readonly IFormatter _keySerializer;
        private readonly IFormatter _valueSerializer;

        private bool _inited;
        private bool _changed;
        private long _position;

        private long[] _pagesPositions;
        private RWayNodesPageBs<TKey, TValue>[] _pages;

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
            const int count = Size * Size;

            for (int i = 0; i < count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private RWayNodeBs<TKey, TValue> ReadNode(int index)
        {
            var page = InitPage(index);
            var nodeIndex = index % Size;

            return page[nodeIndex];
        }

        private void WriteNode(RWayNodeBs<TKey, TValue> node, int index)
        {
            var page = InitPage(index);
            var nodeIndex = index % Size;

            page[nodeIndex] = node;

            _changed = true;
        }

        private void Init()
        {
            if (_inited)
                return;

            _inited = true;

            _pagesPositions = new long[Size];
            _pages = new RWayNodesPageBs<TKey, TValue>[Size];

            if (_position == -1L)
            {
                for (int i = 0; i < Size; i++)
                    _pagesPositions[i] = -1L;

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
                    _pagesPositions[i] = BitConverter.ToInt64(bytes, i * 8);

                //for (int i = 0; i < Size; i++)
                //    _pagesPositions[i] = reader.ReadInt64();
            }
        }

        private RWayNodesPageBs<TKey, TValue> InitPage(int index)
        {
            Init();

            var pageIndex = index / Size;
            
            var page = _pages[pageIndex];
            var pagePosition = _pagesPositions[pageIndex];

            if (page == null)
            {
                page = new RWayNodesPageBs<TKey, TValue>(pagePosition, _stream, _keySerializer, _valueSerializer);
                _pages[pageIndex] = page;
            }

            return page;
        } 

        public void Flush()
        {
            Init();

            for (int i = 0; i < Size; i++)
            {
                var page = _pages[i];
                if (page != null)
                {
                    page.Flush();
                    _pagesPositions[i] = page.Position;
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
                    BufferUtil.Write(bytes, i * 8, _pagesPositions[i]);

                _stream.Write(bytes, 0, bytes.Length);

                _changed = false;

                //var writer = new BinaryWriter(_stream);

                //for (int i = 0; i < Size; i++)
                //    writer.Write(_pagesPositions[i]); 
            }
        }
    }
}

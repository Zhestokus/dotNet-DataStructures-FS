using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using DataStructuresFsConsoleApp.Common;

namespace DataStructuresFsConsoleApp.RedBlack
{
    public class RedBlackTreeBs<TKey, TValue>
    {
        private enum TreeRotation
        {
            LeftRotation = 1,
            RightRotation = 2,
            RightLeftRotation = 3,
            LeftRightRotation = 4,
        }

        private readonly Stream _stream;

        private readonly IFormatter _keySerializer;
        private readonly IFormatter _valueSerializer;

        private readonly IComparer<byte[]> _comparer;

        private RedBlackNodeBs<TKey, TValue> _root;


        private long _rootPosition;
        private int _count;

        public RedBlackTreeBs(Stream stream, bool open)
            : this(stream, new BinaryFormatter(), new BinaryFormatter(), open)
        {
        }

        public RedBlackTreeBs(Stream stream, IFormatter keySerializer, IFormatter valueSerializer, bool open)
        {
            _comparer = new ByteArrayComparer();

            _stream = stream;

            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;

            _stream.Seek(0L, SeekOrigin.Begin);

            if (open)
            {
                var reader = new BinaryReader(_stream);

                _count = reader.ReadInt32();
                _rootPosition = reader.ReadInt64();

                _root = new RedBlackNodeBs<TKey, TValue>(_rootPosition, stream, keySerializer, valueSerializer);
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

        public TKey Min
        {
            get
            {
                var prev = _root;
                var node = _root;

                while (node != null)
                {
                    prev = node;
                    node = node.Left;
                }

                if (prev != null)
                    return prev.Key;

                return default(TKey);
            }
        }

        public TKey Max
        {
            get
            {
                var prev = _root;
                var node = _root;

                while (node != null)
                {
                    prev = node;
                    node = node.Right;
                }

                if (prev != null)
                    return prev.Key;

                return default(TKey);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (_root == null)
            {   // empty tree
                _root = new RedBlackNodeBs<TKey, TValue>(_stream, key, value, RedBlackNodeBs<TKey, TValue>.BLACK, _keySerializer, _valueSerializer);
                _count = 1;

                return;
            }

            //
            // Search for a node at bottom to insert the new node. 
            // If we can guanratee the node we found is not a 4-node, it would be easy to do insertion.
            // We split 4-nodes along the search path.
            // 
            RedBlackNodeBs<TKey, TValue> current = _root;
            RedBlackNodeBs<TKey, TValue> parent = null;
            RedBlackNodeBs<TKey, TValue> grandParent = null;
            RedBlackNodeBs<TKey, TValue> greatGrandParent = null;

            var keyBytes = SerializeKey(key);

            int order = 0;

            while (current != null)
            {
                order = _comparer.Compare(keyBytes, current.KeyBytes);

                if (order == 0)
                {
                    _root.Color = RedBlackNodeBs<TKey, TValue>.BLACK;

                    throw new Exception("Key exists");
                }

                // split a 4-node into two 2-nodes                
                if (Is4Node(current))
                {
                    Split4Node(current);

                    // We could have introduced two consecutive red nodes after split. Fix that by rotation.
                    if (IsRed(parent))
                    {
                        parent = InsertionBalance(current, parent, grandParent, greatGrandParent);
                    }
                }

                greatGrandParent = grandParent;
                grandParent = parent;
                parent = current;

                current = (order < 0) ? current.Left : current.Right;
            }

            // ready to insert the new node
            var node = new RedBlackNodeBs<TKey, TValue>(_stream, key, value, RedBlackNodeBs<TKey, TValue>.RED, _keySerializer, _valueSerializer);
            if (order > 0)
            {
                parent.Right = node;
            }
            else
            {
                parent.Left = node;
            }

            // the new node will be red, so we will need to adjust the colors if parent node is also red
            if (parent.Color == RedBlackNodeBs<TKey, TValue>.RED)
            {
                parent = InsertionBalance(node, parent, grandParent, greatGrandParent);
            }

            // Root node is always black
            _root.Color = RedBlackNodeBs<TKey, TValue>.BLACK;

            _count++;
        }

        public bool Remove(TKey key)
        {
            if (_root == null)
            {
                return false;
            }

            var current = _root;

            var parent = (RedBlackNodeBs<TKey, TValue>)null;
            var grandParent = (RedBlackNodeBs<TKey, TValue>)null;

            var match = (RedBlackNodeBs<TKey, TValue>)null;
            var parentOfMatch = (RedBlackNodeBs<TKey, TValue>)null;

            var foundMatch = false;

            while (current != null)
            {
                if (Is2Node(current))
                {
                    if (parent == null)
                    {
                        current.Color = RedBlackNodeBs<TKey, TValue>.RED;
                    }
                    else
                    {
                        var sibling = GetSibling(current, parent);
                        if (sibling.Color == RedBlackNodeBs<TKey, TValue>.RED)
                        {
                            if (parent.Right == sibling)
                                RotateLeft(parent);
                            else
                                RotateRight(parent);

                            parent.Color = RedBlackNodeBs<TKey, TValue>.RED;
                            sibling.Color = RedBlackNodeBs<TKey, TValue>.BLACK;

                            ReplaceChildOfNodeOrRoot(grandParent, parent, sibling);

                            grandParent = sibling;
                            if (parent == match)
                            {
                                parentOfMatch = sibling;
                            }

                            // update sibling, this is necessary for following processing
                            sibling = (parent.Left == current) ? parent.Right : parent.Left;
                        }

                        if (Is2Node(sibling))
                        {
                            Merge2Nodes(parent, current, sibling);
                        }
                        else
                        {
                            var rotation = RotationNeeded(parent, current, sibling);

                            var newGrandParent = (RedBlackNodeBs<TKey, TValue>)null;
                            switch (rotation)
                            {
                                case TreeRotation.RightRotation:
                                    sibling.Left.Color = RedBlackNodeBs<TKey, TValue>.BLACK;
                                    newGrandParent = RotateRight(parent);
                                    break;
                                case TreeRotation.LeftRotation:
                                    sibling.Right.Color = RedBlackNodeBs<TKey, TValue>.BLACK;
                                    newGrandParent = RotateLeft(parent);
                                    break;

                                case TreeRotation.RightLeftRotation:
                                    newGrandParent = RotateRightLeft(parent);
                                    break;
                                case TreeRotation.LeftRightRotation:
                                    newGrandParent = RotateLeftRight(parent);
                                    break;
                            }

                            newGrandParent.Color = parent.Color;

                            parent.Color = RedBlackNodeBs<TKey, TValue>.BLACK;
                            current.Color = RedBlackNodeBs<TKey, TValue>.RED;

                            ReplaceChildOfNodeOrRoot(grandParent, parent, newGrandParent);

                            if (parent == match)
                                parentOfMatch = newGrandParent;

                            grandParent = newGrandParent;
                        }
                    }
                }

                var keyBytes = SerializeKey(key);

                var order = foundMatch ? -1 : _comparer.Compare(keyBytes, current.KeyBytes);
                if (order == 0)
                {
                    // save the matching node
                    foundMatch = true;
                    match = current;
                    parentOfMatch = parent;
                }

                grandParent = parent;
                parent = current;

                current = (order < 0 ? current.Left : current.Right);
            }

            // move successor to the matching node position and replace links
            if (match != null)
            {
                ReplaceNode(match, parentOfMatch, parent, grandParent);

                _count--;
            }

            if (_root != null)
            {
                _root.Color = RedBlackNodeBs<TKey, TValue>.BLACK;
            }

            return foundMatch;
        }

        public void Clear()
        {
            _root = null;
            _count = 0;
        }

        public bool Contains(TKey item)
        {
            return FindNode(item) != null;
        }

        public RedBlackNodeBs<TKey, TValue> FindNode(TKey key)
        {
            var keyBytes = SerializeKey(key);

            var current = _root;

            while (current != null)
            {
                var order = _comparer.Compare(keyBytes, current.KeyBytes);
                if (order == 0)
                    return current;

                current = (order < 0) ? current.Left : current.Right;
            }

            return null;
        }
        public RedBlackNodeBs<TKey, TValue> FindRange(TKey fromKey, TKey toKey)
        {
            return FindRange(fromKey, toKey, true, true);
        }
        public RedBlackNodeBs<TKey, TValue> FindRange(TKey fromKey, TKey toKey, bool lowerBoundActive, bool upperBoundActive)
        {
            var fromKeyBytes = SerializeKey(fromKey);
            var toKeyBytes = SerializeKey(toKey);

            var current = _root;

            while (current != null)
            {
                if (lowerBoundActive && _comparer.Compare(fromKeyBytes, current.KeyBytes) > 0)
                {
                    current = current.Right;
                }
                else
                {
                    if (upperBoundActive && _comparer.Compare(toKeyBytes, current.KeyBytes) < 0)
                        current = current.Left;
                    else
                        return current;
                }
            }

            return null;
        }

        public void Flush()
        {
            const int start = sizeof(int) + sizeof(long);
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

        private RedBlackNodeBs<TKey, TValue> GetSibling(RedBlackNodeBs<TKey, TValue> node, RedBlackNodeBs<TKey, TValue> parent)
        {
            return (parent.Left == node ? parent.Right : parent.Left);
        }
        //private RedBlackNodeBs<TKey, TValue> InsertionBalance(RedBlackNodeBs<TKey, TValue> current, RedBlackNodeBs<TKey, TValue> parent, RedBlackNodeBs<TKey, TValue> grandParent, RedBlackNodeBs<TKey, TValue> greatGrandParent)
        //{
        //    var parentIsOnRight = (grandParent.Right == parent);
        //    var currentIsOnRight = (parent.Right == current);

        //    RedBlackNodeBs<TKey, TValue> newChildOfGreatGrandParent;

        //    if (parentIsOnRight == currentIsOnRight)
        //    {
        //        newChildOfGreatGrandParent = currentIsOnRight ? RotateLeft(grandParent) : RotateRight(grandParent);
        //    }
        //    else
        //    {
        //        newChildOfGreatGrandParent = currentIsOnRight ? RotateLeftRight(grandParent) : RotateRightLeft(grandParent);
        //        parent = greatGrandParent;
        //    }

        //    grandParent.Color = RedBlackNodeBs<TKey, TValue>.RED;
        //    newChildOfGreatGrandParent.Color = RedBlackNodeBs<TKey, TValue>.BLACK;

        //    ReplaceChildOfNodeOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent);

        //    return parent;
        //}

        private RedBlackNodeBs<TKey, TValue> InsertionBalance(RedBlackNodeBs<TKey, TValue> current, RedBlackNodeBs<TKey, TValue> parent, RedBlackNodeBs<TKey, TValue> grandParent, RedBlackNodeBs<TKey, TValue> greatGrandParent)
        {
            var parentIsOnRight = (grandParent.Right == parent);
            var currentIsOnRight = (parent.Right == current);

            RedBlackNodeBs<TKey, TValue> newChildOfGreatGrandParent;

            if (parentIsOnRight == currentIsOnRight)
            { // same orientation, single rotation
                newChildOfGreatGrandParent = currentIsOnRight ? RotateLeft(grandParent) : RotateRight(grandParent);
            }
            else
            {  // different orientaton, double rotation
                newChildOfGreatGrandParent = currentIsOnRight ? RotateLeftRight(grandParent) : RotateRightLeft(grandParent);
                // current node now becomes the child of greatgrandparent 
                parent = greatGrandParent;
            }

            // grand parent will become a child of either parent of current.
            grandParent.Color = RedBlackNodeBs<TKey, TValue>.RED;
            newChildOfGreatGrandParent.Color = RedBlackNodeBs<TKey, TValue>.BLACK;

            ReplaceChildOfNodeOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent);

            return parent;
        }

        private bool Is2Node(RedBlackNodeBs<TKey, TValue> node)
        {
            return IsBlack(node) && IsNullOrBlack(node.Left) && IsNullOrBlack(node.Right);
        }
        private bool Is4Node(RedBlackNodeBs<TKey, TValue> node)
        {
            return IsRed(node.Left) && IsRed(node.Right);
        }

        private bool IsRed(RedBlackNodeBs<TKey, TValue> node)
        {
            return (node != null && node.Color == RedBlackNodeBs<TKey, TValue>.RED);
        }
        private bool IsBlack(RedBlackNodeBs<TKey, TValue> node)
        {
            return (node != null && node.Color == RedBlackNodeBs<TKey, TValue>.BLACK);
        }
        private bool IsNullOrBlack(RedBlackNodeBs<TKey, TValue> node)
        {
            return (node == null || node.Color == RedBlackNodeBs<TKey, TValue>.BLACK);
        }

        private void Split4Node(RedBlackNodeBs<TKey, TValue> node)
        {
            node.Color = RedBlackNodeBs<TKey, TValue>.RED;
            node.Left.Color = RedBlackNodeBs<TKey, TValue>.BLACK;
            node.Right.Color = RedBlackNodeBs<TKey, TValue>.BLACK;
        }

        private void Merge2Nodes(RedBlackNodeBs<TKey, TValue> parent, RedBlackNodeBs<TKey, TValue> child1, RedBlackNodeBs<TKey, TValue> child2)
        {
            parent.Color = RedBlackNodeBs<TKey, TValue>.BLACK;

            child1.Color = RedBlackNodeBs<TKey, TValue>.RED;
            child2.Color = RedBlackNodeBs<TKey, TValue>.RED;
        }
        private void ReplaceNode(RedBlackNodeBs<TKey, TValue> match, RedBlackNodeBs<TKey, TValue> parentOfMatch, RedBlackNodeBs<TKey, TValue> succesor, RedBlackNodeBs<TKey, TValue> parentOfSuccesor)
        {
            if (succesor == match)
            {
                succesor = match.Left;
            }
            else
            {
                if (succesor.Right != null)
                    succesor.Right.Color = RedBlackNodeBs<TKey, TValue>.BLACK;

                if (parentOfSuccesor != match)
                {
                    parentOfSuccesor.Left = succesor.Right;
                    succesor.Right = match.Right;
                }

                succesor.Left = match.Left;
            }

            if (succesor != null)
                succesor.Color = match.Color;

            ReplaceChildOfNodeOrRoot(parentOfMatch, match, succesor);
        }

        private void ReplaceChildOfNodeOrRoot(RedBlackNodeBs<TKey, TValue> parent, RedBlackNodeBs<TKey, TValue> child, RedBlackNodeBs<TKey, TValue> newChild)
        {
            if (parent != null)
            {
                if (parent.Left == child)
                {
                    parent.Left = newChild;
                }
                else
                {
                    parent.Right = newChild;
                }
            }
            else
            {
                _root = newChild;
            }
        }

        private RedBlackNodeBs<TKey, TValue> RotateLeft(RedBlackNodeBs<TKey, TValue> node)
        {
            var x = node.Right;

            node.Right = x.Left;
            x.Left = node;

            return x;
        }
        private RedBlackNodeBs<TKey, TValue> RotateRight(RedBlackNodeBs<TKey, TValue> node)
        {
            var x = node.Left;

            node.Left = x.Right;
            x.Right = node;

            return x;
        }

        private RedBlackNodeBs<TKey, TValue> RotateLeftRight(RedBlackNodeBs<TKey, TValue> node)
        {
            var child = node.Left;
            var grandChild = child.Right;

            node.Left = grandChild.Right;
            grandChild.Right = node;
            child.Right = grandChild.Left;
            grandChild.Left = child;
            return grandChild;
        }
        private RedBlackNodeBs<TKey, TValue> RotateRightLeft(RedBlackNodeBs<TKey, TValue> node)
        {
            var child = node.Right;
            var grandChild = child.Left;

            node.Right = grandChild.Left;
            grandChild.Left = node;
            child.Left = grandChild.Right;
            grandChild.Right = child;
            return grandChild;
        }

        private TreeRotation RotationNeeded(RedBlackNodeBs<TKey, TValue> parent, RedBlackNodeBs<TKey, TValue> current, RedBlackNodeBs<TKey, TValue> sibling)
        {
            if (IsRed(sibling.Left))
            {
                if (parent.Left == current)
                    return TreeRotation.RightLeftRotation;

                return TreeRotation.RightRotation;
            }

            if (parent.Left == current)
                return TreeRotation.LeftRotation;

            return TreeRotation.LeftRightRotation;
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
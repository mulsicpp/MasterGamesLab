using System;
using System.Collections.Generic;

namespace Map
{
    /// <summary>
    /// A lightweight, ultra-fast Min-Heap Priority Queue for Unity.
    /// </summary>
    public class PriorityQueue<TElement, TPriority>
        where TPriority : struct, IComparable<TPriority>
    {
        private readonly List<(TElement Element, TPriority Priority)> _nodes = new();

        public int Count => _nodes.Count;

        public void Enqueue(TElement element, TPriority priority)
        {
            _nodes.Add((element, priority));
            BubbleUp(_nodes.Count - 1);
        }

        public TElement Dequeue()
        {
            if (_nodes.Count == 0) throw new InvalidOperationException("Queue is empty");

            TElement root = _nodes[0].Element;
            int lastIdx = _nodes.Count - 1;

            _nodes[0] = _nodes[lastIdx];
            _nodes.RemoveAt(lastIdx);

            if (_nodes.Count > 0)
            {
                TrickleDown(0);
            }

            return root;
        }

        public void Clear()
        {
            _nodes.Clear();
        }

        private int CompareNodes(int indexA, int indexB)
        {
            return _nodes[indexA].Priority.CompareTo(_nodes[indexB].Priority);
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parentIdx = (index - 1) / 2;

                if (CompareNodes(index, parentIdx) >= 0) break;

                Swap(index, parentIdx);
                index = parentIdx;
            }
        }

        private void TrickleDown(int index)
        {
            int count = _nodes.Count;
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int smallest = index;

                if (leftChild < count && CompareNodes(leftChild, smallest) < 0)
                    smallest = leftChild;

                if (rightChild < count && CompareNodes(rightChild, smallest) < 0)
                    smallest = rightChild;

                if (smallest == index) break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int i, int j)
        {
            var temp = _nodes[i];
            _nodes[i] = _nodes[j];
            _nodes[j] = temp;
        }
    }
}
using System;
using System.Collections.Generic;

namespace ADCREA.Algorithms
{

    public class MinHeap<T>
    {
        private readonly List<T> _items = new List<T>();
        private readonly List<float> _priorities = new List<float>();

        public int Count
        {
            get { return _items.Count; }
        }

        public void Push(T item, float priority)
        {
            _items.Add(item);
            _priorities.Add(priority);
            SiftUp(_items.Count - 1);
        }

        public T Pop()
        {
            if (_items.Count == 0) throw new InvalidOperationException("Heap is empty.");
            T root = _items[0];
            int last = _items.Count - 1;
            _items[0] = _items[last];
            _priorities[0] = _priorities[last];
            _items.RemoveAt(last);
            _priorities.RemoveAt(last);
            if (_items.Count > 0) SiftDown(0);
            return root;
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_priorities[i] >= _priorities[parent]) break;
                Swap(i, parent);
                i = parent;
            }
        }

        private void SiftDown(int i)
        {
            int n = _items.Count;
            while (true)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;
                if (left < n && _priorities[left] < _priorities[smallest]) smallest = left;
                if (right < n && _priorities[right] < _priorities[smallest]) smallest = right;
                if (smallest == i) break;
                Swap(i, smallest);
                i = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            (_items[a], _items[b]) = (_items[b], _items[a]);
            (_priorities[a], _priorities[b]) = (_priorities[b], _priorities[a]);
        }
    }
}

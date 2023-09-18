namespace Durandal.Common.Logger
{
    using System.Collections.Generic;
    using System.Collections;

    public class LoggingHistory : IEnumerable
    {
        private int _listSize = 0;
        private int _backlogSize;

        private LinkedListNode _first;
        private LinkedListNode _last;

        public LoggingHistory(int backlogSize = 1000)
        {
            _backlogSize = backlogSize;
        }

        public void Add(LogEvent value)
        {
            lock (this)
            {
                while (this._listSize > this._backlogSize)
                {
                    this._first = this._first.Next;
                    this._first.Prev.Next = null;
                    this._first.Prev = null;
                    this._listSize -= 1;
                }
                LinkedListNode newNode = new LinkedListNode(value);
                if (this._first == null)
                {
                    // Start a new list
                    this._first = newNode;
                    this._last = newNode;
                }
                else
                {
                    // Append to existing list
                    newNode.Prev = this._last;
                    if (this._last != null)
                    {
                        this._last.Next = newNode;
                    }
                    this._last = newNode;
                }
                this._listSize += 1;
            }
        }

        public IEnumerator GetEnumerator()
        {
            lock (this)
            {
                return new EventEnumerator(this._first);
            }
        }

        public IEnumerable<LogEvent> FilterByCriteria(LogLevel level, bool iterateReverse = false)
        {
            return this.FilterByCriteria(new FilterCriteria() { Level = level }, iterateReverse);
        }

        public IEnumerable<LogEvent> FilterByCriteria(FilterCriteria criteria, bool iterateReverse = false)
        {
            lock (this)
            {
                if (iterateReverse)
                    return new EnumerableImpl(new EventEnumerator(this._last, criteria, false));
                else
                    return new EnumerableImpl(new EventEnumerator(this._first, criteria, true));
            }
        }

        /// <summary>
        /// A simple converter from IEnumerator to IEnumerable
        /// </summary>
        private class EnumerableImpl : IEnumerable<LogEvent>
        {
            private readonly IEnumerator<LogEvent> _enumerator;

            public EnumerableImpl(IEnumerator<LogEvent> enumerator)
            {
                this._enumerator = enumerator;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this._enumerator;
            }

            public IEnumerator<LogEvent> GetEnumerator()
            {
                return this._enumerator;
            }
        }

        /// <summary>
        /// An enumerator which goes forward or backwards over log history
        /// </summary>
        private class EventEnumerator : IEnumerator<LogEvent>
        {
            private readonly LinkedListNode _firstNode;
            private LinkedListNode _node;
            private FilterCriteria _filter;
            private bool _forward;

            public EventEnumerator(LinkedListNode head, FilterCriteria criteria = null, bool forward = true)
            {
                this._node = head;
                this._firstNode = head;
                this._filter = criteria;
                this._forward = forward;
            }
            
            public LogEvent Current
            {
                get
                {
                    if (this._node == null)
                        return null;
                    return this._node.Event;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (this._node == null)
                        return null;
                    return this._node.Event;
                }
            }

            public void Reset()
            {
                this._node = this._firstNode;
            }

            public bool MoveNext()
            {
                if (this._node == null)
                    return false;

                bool passedCriteria = false;
                while (!passedCriteria)
                {
                    if (this._forward)
                        this._node = this._node.Next;
                    else
                        this._node = this._node.Prev;
                    if (this._node == null)
                        return false;
                    passedCriteria = this._filter == null || this._filter.PassesFilter(this._node.Event);
                }

                return true;
            }

            public void Dispose() { }
        }

        /// <summary>
        /// An element of the log history linked list
        /// </summary>
        private class LinkedListNode
        {
            public readonly LogEvent Event;
            public volatile LinkedListNode Next = null;
            public volatile LinkedListNode Prev = null;

            public LinkedListNode(LogEvent value)
            {
                this.Event = value;
            }
        }
    }
}

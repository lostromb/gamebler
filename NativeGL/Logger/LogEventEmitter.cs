﻿namespace Durandal.Common.Logger
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class handles firing LogUpdatedEvents in a way that provides many-to-many mappings from loggers to consumers
    /// </summary>
    public class LogEventEmitter
    {
        private readonly ISet<EventHandler<LogUpdatedEventArgs>> _handlers;

        public LogEventEmitter()
        {
            this._handlers = new HashSet<EventHandler<LogUpdatedEventArgs>>();
        }
        
        /// <summary>
        /// Add a new handler that listens to aggregated events sent through this emitter
        /// </summary>
        /// <param name="handler"></param>
        public void Add(EventHandler<LogUpdatedEventArgs> handler)
        {
            if (handler == null)
            {
                return;
            }

            lock (this)
            {
                if (!this._handlers.Contains(handler))
                {
                    this._handlers.Add(handler);
                }
            }
        }

        /// <summary>
        /// Remove a handler for events generated by this emitter
        /// </summary>
        /// <param name="handler"></param>
        public void Remove(EventHandler<LogUpdatedEventArgs> handler)
        {
            if (handler == null)
            {
                return;
            }

            lock (this)
            {
                if (this._handlers.Contains(handler))
                {
                    this._handlers.Remove(handler);
                }
            }
        }

        public void Fire(object sender, LogUpdatedEventArgs args)
        {
            lock (this)
            {
                foreach (var handler in this._handlers)
                {
                    handler(sender, args);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeUtils
{
    public enum Format
    {
        Milliseconds,
        Seconds
    }
    
    public class EventSpan
    {
        private readonly long _startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        private long _endTime;

        private bool _isDone;

        public bool IsDone()
        {
            return _isDone;
        }

        public void End()
        {
            if (_isDone)
                throw new SystemException();
            
            _isDone = true;
            _endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public long GetDuration()
        {
            if (!_isDone)
                throw new SystemException();

            return _endTime - _startTime;
        }
    }
    
    public static class Logger
    {
        private static readonly Dictionary<Enum, EventSpan> Events = new();
        
        public static void StartEvent(Enum eventType)
        {
            if (Events.ContainsKey(eventType))
                throw new SystemException();
            
            Events.Add(eventType, new EventSpan());
        }

        public static void EndEvent(Enum eventType, Format format)
        {
            EventSpan span = Events[eventType];
            span.End();

            float divider = format switch
            {
                Format.Milliseconds => 1,
                Format.Seconds => 1000,
                _ => throw new SystemException()
            };
            Debug.Log($"{eventType.ToString()} took: {span.GetDuration() / divider}");
        }

        public static bool IsEventStarted(Enum eventType)
        {
            return Events.ContainsKey(eventType);
        }

        public static bool IsEventDone(Enum eventType)
        {
            if (!Events.ContainsKey(eventType))
                throw new SystemException();

            return Events[eventType].IsDone();
        }

        public static bool IsEventRunning(Enum eventType)
        {
            return IsEventStarted(eventType) && !IsEventDone(eventType);
        }
    }
}
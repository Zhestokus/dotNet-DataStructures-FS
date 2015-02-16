using System;

namespace DataStructuresFsConsoleApp
{
    public class LogEntry
    {
        public TimeSpan TotalTime { get; set; }

        public TimeSpan InsertTime { get; set; }
        public TimeSpan TrieFlushTime { get; set; }
        public TimeSpan StreamFlushTime { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using DataStructuresFsConsoleApp.Common;
using DataStructuresFsConsoleApp.RedBlack;
using DataStructuresFsConsoleApp.RWay;
using DataStructuresFsConsoleApp.RWaySe;
using DataStructuresFsConsoleApp.Terany;

namespace DataStructuresFsConsoleApp
{
    class Program
    {
        private const String redBlackFileName = @"rbData.dat";
        private const String teranyTrieFileName = @"TrData.dat";
        private const String rwayTrieFileName = @"RwData.dat";
        private const String rwayTrieSeFileName = @"RwSeData.dat";

        static void Main(string[] args)
        {
            var redBlackStream = File.Open(redBlackFileName, FileMode.Create, FileAccess.ReadWrite);
            var teranyTrieStream = File.Open(teranyTrieFileName, FileMode.Create, FileAccess.ReadWrite);
            var rwayTrieStream = File.Open(rwayTrieFileName, FileMode.Create, FileAccess.ReadWrite);
            var rwayTrieSeStream = File.Open(rwayTrieSeFileName, FileMode.Create, FileAccess.ReadWrite);

            Initialize(redBlackStream, teranyTrieStream, rwayTrieStream, rwayTrieSeStream);

            var count = 0;

            for (int i = 0; i < 1000; i++)
            {
                var dictionary = new Dictionary<Guid, String>();

                for (int j = 0; j < 1000; j++)
                {
                    var key = Guid.NewGuid();
                    var value = Convert.ToString(key);

                    dictionary.Add(key, value);
                }

                count += dictionary.Count;

                var redBlackTreeLogEntity = TestRedBlack(redBlackStream, dictionary);
                Print("RedBlackTree", redBlackTreeLogEntity, count);

                var rwayTrieLogEntity = TestRWayTrie(rwayTrieStream, dictionary);
                Print("RWayTrie", rwayTrieLogEntity, count);

                var rwayTrieSeLogEntity = TestRWayTrieSe(rwayTrieSeStream, dictionary);
                Print("RWayTrieSe", rwayTrieSeLogEntity, count);

                var teranyTrieLogEntity = TestTeranyTrie(teranyTrieStream, dictionary);
                Print("TeranyTrie", teranyTrieLogEntity, count);

                Console.WriteLine();
            }
        }

        static void Print(String prefix, LogEntry logEntry, int count)
        {
            Console.WriteLine("{0,-6}: COUNT: {1,-6}; TOTAL: {2,-6}; INSERT: {3,-6}; TRIE/TREE FLUSH: {4,-6}; STREAM FLUSH: {5,-6}",
                              prefix,
                              count,
                              (int)logEntry.TotalTime.TotalMilliseconds,
                              (int)logEntry.InsertTime.TotalMilliseconds,
                              (int)logEntry.TrieFlushTime.TotalMilliseconds,
                              (int)logEntry.StreamFlushTime.TotalMilliseconds);
        }

        static LogEntry TestRedBlack(Stream stream, Dictionary<Guid, String> dictionary)
        {
            var totalSw = Stopwatch.StartNew();

            var bufferedStream = new BufferMemoryStream(stream);
            var tree = new RedBlackTreeBs<Guid, String>(bufferedStream, true);

            var insertSw = new Stopwatch();
            foreach (var pair in dictionary)
            {
                insertSw.Start();
                tree.Add(pair.Key, pair.Value);
                insertSw.Stop();

                Application.DoEvents();
            }

            var trieFlushSw = Stopwatch.StartNew();
            tree.Flush();
            trieFlushSw.Stop();

            var streamFlushSw = Stopwatch.StartNew();
            bufferedStream.Flush();
            streamFlushSw.Stop();

            var logEntry = new LogEntry
            {
                InsertTime = insertSw.Elapsed,
                TrieFlushTime = trieFlushSw.Elapsed,
                StreamFlushTime = trieFlushSw.Elapsed,
            };

            totalSw.Stop();

            logEntry.TotalTime = totalSw.Elapsed;

            return logEntry;
        }

        static LogEntry TestTeranyTrie(Stream stream, Dictionary<Guid, String> dictionary)
        {
            var totalSw = Stopwatch.StartNew();

            var bufferedStream = new BufferMemoryStream(stream);
            var trie = new TeranyTrieBs<Guid, String>(bufferedStream, true);

            var insertSw = new Stopwatch();
            foreach (var pair in dictionary)
            {
                insertSw.Start();
                trie.Add(pair.Key, pair.Value);
                insertSw.Stop();

                Application.DoEvents();
            }

            var trieFlushSw = Stopwatch.StartNew();
            trie.Flush();
            trieFlushSw.Stop();

            var streamFlushSw = Stopwatch.StartNew();
            bufferedStream.Flush();
            streamFlushSw.Stop();

            var logEntry = new LogEntry
            {
                InsertTime = insertSw.Elapsed,
                TrieFlushTime = trieFlushSw.Elapsed,
                StreamFlushTime = trieFlushSw.Elapsed,
            };

            totalSw.Stop();

            logEntry.TotalTime = totalSw.Elapsed;

            return logEntry;
        }

        static LogEntry TestRWayTrie(Stream stream, Dictionary<Guid, String> dictionary)
        {
            var totalSw = Stopwatch.StartNew();

            var bufferedStream = new BufferMemoryStream(stream);
            var trie = new RWayTrieBs<Guid, String>(bufferedStream, true);

            var insertSw = new Stopwatch();
            foreach (var pair in dictionary)
            {
                insertSw.Start();
                trie.Add(pair.Key, pair.Value);
                insertSw.Stop();

                Application.DoEvents();
            }

            var trieFlushSw = Stopwatch.StartNew();
            trie.Flush();
            trieFlushSw.Stop();

            var streamFlushSw = Stopwatch.StartNew();
            bufferedStream.Flush();
            streamFlushSw.Stop();

            var logEntry = new LogEntry
            {
                InsertTime = insertSw.Elapsed,
                TrieFlushTime = trieFlushSw.Elapsed,
                StreamFlushTime = trieFlushSw.Elapsed,
            };

            totalSw.Stop();

            logEntry.TotalTime = totalSw.Elapsed;

            return logEntry;

        }

        static LogEntry TestRWayTrieSe(Stream stream, Dictionary<Guid, String> dictionary)
        {
            var totalSw = Stopwatch.StartNew();

            var bufferedStream = new BufferMemoryStream(stream);
            var trie = new RWayTrieSeBs<Guid, String>(bufferedStream, true);

            var insertSw = new Stopwatch();
            foreach (var pair in dictionary)
            {
                insertSw.Start();
                trie.Add(pair.Key, pair.Value);
                insertSw.Stop();

                Application.DoEvents();
            }

            var trieFlushSw = Stopwatch.StartNew();
            trie.Flush();
            trieFlushSw.Stop();

            var streamFlushSw = Stopwatch.StartNew();
            bufferedStream.Flush();
            streamFlushSw.Stop();

            var logEntry = new LogEntry
            {
                InsertTime = insertSw.Elapsed,
                TrieFlushTime = trieFlushSw.Elapsed,
                StreamFlushTime = trieFlushSw.Elapsed,
            };

            totalSw.Stop();

            logEntry.TotalTime = totalSw.Elapsed;

            return logEntry;
        }

        private static void Initialize(FileStream redBlackStream, FileStream teranyTrieStream, FileStream rwayTrieStream, FileStream rwayTrieSeStream)
        {
            var key = Guid.NewGuid();
            var value = Convert.ToString(key);

            var rwayTrie = new RWayTrieBs<Guid, String>(rwayTrieStream, false);
            rwayTrie.Add(key, value);
            rwayTrie.Flush();

            var redBlackTree = new RedBlackTreeBs<Guid, String>(redBlackStream, false);
            redBlackTree.Add(key, value);
            redBlackTree.Flush();

            var teranyTrie = new TeranyTrieBs<Guid, String>(teranyTrieStream, false);
            teranyTrie.Add(key, value);
            teranyTrie.Flush();

            var rwayTrieSe = new RWayTrieSeBs<Guid, String>(rwayTrieSeStream, false);
            rwayTrieSe.Add(key, value);
            rwayTrieSe.Flush();


            redBlackStream.Flush();
            teranyTrieStream.Flush();
            rwayTrieStream.Flush();
            rwayTrieSeStream.Flush();
        }
    }
}

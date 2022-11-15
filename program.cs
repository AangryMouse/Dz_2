using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MultyLock
{
    public interface IMultiLock
    {
        public IDisposable AcquireLock(params string[] keys);
    }

    class MultiLock : IMultiLock
    {
        private object lockObj = new();
        private Dictionary<string, object> monitorLocks;

        public MultiLock() => monitorLocks = new Dictionary<string, object>();
        
        private void ReleaseKey(string key) => Monitor.Exit(monitorLocks[key]);
        
        private void TakeKey(string key)
        {
            lock (lockObj)
                if (!monitorLocks.ContainsKey(key))
                    monitorLocks[key] = new object();

            Monitor.Enter(monitorLocks[key]);
        }

        public IDisposable AcquireLock(params string[] keys)
        {
            var keysTaken = false;
            try
            {
                foreach (var wantedKey in keys.OrderBy(k => k))
                {
                    TakeKey(wantedKey);
                }
                keysTaken = true;
                return new Disposer(keys, monitorLocks);
            }
            finally
            {
                if (!keysTaken)
                {
                    var keysLocks = keys
                        .Where(k => Monitor.IsEntered(monitorLocks[k]))
                        .OrderBy(x => x)
                        .Reverse();
                    foreach (var key in keysLocks)
                    {
                        ReleaseKey(key);
                    }
                }
            }
        }
    }

    public class Disposer : IDisposable
    {
        private Dictionary<string, object> lockDictionary;
        private IEnumerable<string> keys;
        public Disposer(IEnumerable<string> keys, Dictionary<string, object> lockDictionary)
        {
            this.lockDictionary = lockDictionary;
            this.keys = keys;
        }

        public void Dispose()
        {
            foreach (var key in keys.OrderBy(x => x).Reverse())
            {
                var lockFlag = lockDictionary[key];
                if (!Monitor.IsEntered(lockFlag)) continue;
                Monitor.Exit(lockFlag);
            }
        }
    }
}
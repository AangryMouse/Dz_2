using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Dictionary<string, object> monitorLocks = new ();

        public MultiLock(params string[] keys)
        {
            foreach (var key in keys)
                if (!monitorLocks.ContainsKey(key))
                    monitorLocks[key] = new object();
        }
        
        private void ReleaseKey(string key) => Monitor.Exit(monitorLocks[key]);

        public IDisposable AcquireLock(params string[] keys)
        {
            try
            {
                foreach (var wantedKey in keys)
                {
                    if (!monitorLocks.ContainsKey(wantedKey))
                        Monitor.Enter(wantedKey);
                }

                return new Disposer(keys.Reverse(), monitorLocks);
            }
            catch
            {
                foreach (var key in keys.Reverse())
                {
                    if (Monitor.IsEntered(monitorLocks[key]))
                        ReleaseKey(key);
                }

                throw;
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
            foreach (var key in keys)
            {
                var lockFlag = lockDictionary[key];
                if (!Monitor.IsEntered(lockFlag)) continue;
                Monitor.Exit(lockFlag);
            }
        }
    }
}

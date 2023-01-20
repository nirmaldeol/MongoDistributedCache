using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace MongoCache
{
    public class MongoDbCache : IDistributedCache
    {
        internal DateTimeOffset _lastScan = DateTimeOffset.UtcNow;
        internal TimeSpan _scanInterval;
        internal readonly TimeSpan _defaultScanInterval = TimeSpan.FromMinutes(5);
        internal readonly MongoContext _mongoContext;

        private static void ValidateOptions(MongoDbCacheOptions cacheOptions)
        {
            if (!string.IsNullOrEmpty(cacheOptions.ConnectionString) && cacheOptions.MongoClientSettings != null)
                throw new ArgumentException($"Only one of {nameof(cacheOptions.ConnectionString)} and {nameof(cacheOptions.MongoClientSettings)} can be set.");
            
            if (string.IsNullOrEmpty(cacheOptions.ConnectionString) && cacheOptions.MongoClientSettings == null)
                throw new ArgumentException($"{nameof(cacheOptions.ConnectionString)} or {nameof(cacheOptions.MongoClientSettings)} cannot be empty or null.");

            if (string.IsNullOrEmpty(cacheOptions.DatabaseName))
                throw new ArgumentException($"{nameof(cacheOptions.DatabaseName)} cannot be empty or null.");

            if (string.IsNullOrEmpty(cacheOptions.CollectionName))
                throw new ArgumentException($"{nameof(cacheOptions.CollectionName)} cannot be empty or null.");
        }

        private void SetScanInterval(TimeSpan? scanInterval)
        {
            _scanInterval = scanInterval?.TotalSeconds > 0
                ? scanInterval.Value
                : _defaultScanInterval;
        }

        public MongoDbCache(IOptions<MongoDbCacheOptions> optionsAccessor)
        {
            var options = optionsAccessor.Value;
            ValidateOptions(options);

            _mongoContext = new MongoContext(options.ConnectionString, options.MongoClientSettings, options.DatabaseName, options.CollectionName);
            
            SetScanInterval(options.ExpiredScanInterval);
        }

        public byte[] Get(string key)
        {
            var value = _mongoContext.GetCacheItem(key, withoutValue: false);

            ScanAndDeleteExpired();

            return value;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options = null)
        {
            _mongoContext.Set(key, value, options);

            ScanAndDeleteExpired();
        }

        public void Refresh(string key)
        {
            _mongoContext.GetCacheItem(key, withoutValue: true);

            ScanAndDeleteExpired();
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            var value = await _mongoContext.GetCacheItemAsync(key, withoutValue: false, token: token);

            ScanAndDeleteExpired();

            return value;
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            await _mongoContext.SetAsync(key, value, options, token);

            ScanAndDeleteExpired();
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            await _mongoContext.GetCacheItemAsync(key, withoutValue: true, token: token);

            ScanAndDeleteExpired();
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            await _mongoContext.RemoveAsync(key, token);

            ScanAndDeleteExpired();
        }

        public void Remove(string key)
        {
            _mongoContext.Remove(key);

            ScanAndDeleteExpired();
        }

        internal void ScanAndDeleteExpired()
        {
            var utcNow = DateTimeOffset.UtcNow;

            if (_lastScan.Add(_scanInterval) < utcNow)
                Task.Run(() =>
                {
                    _lastScan = utcNow;
                    _mongoContext.DeleteExpired(utcNow);
                });
        }

        public async Task<byte[][]> HashMemberGetMany(
        string[] keys, bool withoutValue)
        {
            var tasks = new Task<byte[]>[keys.Length];
            var results = new byte[keys.Length][];

            for (var i = 0; i < keys.Length; i++)
            {
                tasks[i] =  _mongoContext.GetCacheItemAsync(keys[i], withoutValue);
            }

            for (var i = 0; i < tasks.Length; i++)
            {
                results[i] = await tasks[i];
            }
            ScanAndDeleteExpired();
            return results;
        }

        public async Task<byte[][]> HashMemberGetManyAsync(
            string[] keys,bool withoutValue, CancellationToken token = default)
        {
            var tasks = new Task<byte[]>[keys.Length];

            for (var i = 0; i < keys.Length; i++)
            {
                tasks[i] = _mongoContext.GetCacheItemAsync(keys[i], withoutValue, token);
            }
            ScanAndDeleteExpired();
            return await Task.WhenAll(tasks);
        }

        public virtual void PipelineSetMany(
            IEnumerable<KeyValuePair<string, byte[]>> items, string Instance,
            DistributedCacheEntryOptions options)
        {
            var itemArray = items.ToArray();

            var tasks = new Task[itemArray.Length];

            for (var i = 0; i < itemArray.Length; i++)
            {
                var key = Instance + itemArray[i].Key;
               _mongoContext.Set(key, itemArray[i].Value, options);
            }

        }

        public virtual Task[] PipelineSetManyAsync(
            IEnumerable<KeyValuePair<string, byte[]>> items, string Instance,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            var itemArray = items.ToArray();

            var tasks = new Task[itemArray.Length];

            for (var i = 0; i < itemArray.Length; i++)
            {
                var key = Instance + itemArray[i].Key;
                tasks[i] = _mongoContext.SetAsync(key, itemArray[i].Value, options,token);
            }
            return tasks;
        }
    }
}
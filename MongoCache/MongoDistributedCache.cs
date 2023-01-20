using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Volo.Abp;
using Volo.Abp.Caching;
using MongoCache;

namespace MongoCache
{
    public class MongoDistributedCache : MongoDbCache, ICacheSupportsMultipleItems
    {

        protected string Instance { get; }

        static MongoDistributedCache()
        {
           
        }

        public MongoDistributedCache(IOptions<MongoDbCacheOptions> optionsAccessor)
            : base(optionsAccessor)
        {
            Instance = optionsAccessor.Value.InstanceName ?? string.Empty;
        }

        public byte[][] GetMany(
            IEnumerable<string> keys)
        {
            keys = Check.NotNull(keys, nameof(keys));

            return GetAndRefreshMany(keys, true);
        }

        public async Task<byte[][]> GetManyAsync(
            IEnumerable<string> keys,
            CancellationToken token = default)
        {
            keys = Check.NotNull(keys, nameof(keys));
            return await GetAndRefreshManyAsync(keys, true, token);
        }

        public void SetMany(
            IEnumerable<KeyValuePair<string, byte[]>> items,
            DistributedCacheEntryOptions options)
        {
            PipelineSetMany(items, Instance, options);
        }

        public async Task SetManyAsync(
            IEnumerable<KeyValuePair<string, byte[]>> items,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await Task.WhenAll(PipelineSetManyAsync(items, Instance, options));
        }

        public void RefreshMany(
            IEnumerable<string> keys)
        {
            keys = Check.NotNull(keys, nameof(keys));

            GetAndRefreshMany(keys, false);
        }

        public async Task RefreshManyAsync(
            IEnumerable<string> keys,
            CancellationToken token = default)
        {
            keys = Check.NotNull(keys, nameof(keys));

            await GetAndRefreshManyAsync(keys, false, token);
        }

        public void RemoveMany(IEnumerable<string> keys)
        {
            keys = Check.NotNull(keys, nameof(keys));
            _mongoContext.RemoveMany(keys.Select(key => (Instance + key)).ToArray());
        }

        public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken token = default)
        {
            keys = Check.NotNull(keys, nameof(keys));
            token.ThrowIfCancellationRequested();
            await _mongoContext.RemoveManyAsync(keys.Select(key => (Instance + key)).ToArray());
        }

        protected virtual byte[][] GetAndRefreshMany(
            IEnumerable<string> keys,
            bool getData)
        {
            var keyArray = keys.Select(key => Instance + key).ToArray();
            byte[][] results;

            if (getData)
            {
                results =  HashMemberGetManyAsync(keyArray,!getData).Result;
            }
            else
            {
                results =  HashMemberGetManyAsync(keyArray,true).Result;
            }

            return results;
        }

        protected virtual async Task<byte[][]> GetAndRefreshManyAsync(
            IEnumerable<string> keys,
            bool getData,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            var keyArray = keys.Select(key => Instance + key).ToArray();
            byte[][] results;

            if (getData)
            {
                results = await HashMemberGetManyAsync(keyArray,!getData, token);
            }
            else
            {
                results = await HashMemberGetManyAsync(keyArray, true);
            }

            return results;
        }

    }
}

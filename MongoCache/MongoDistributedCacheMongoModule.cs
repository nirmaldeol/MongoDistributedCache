using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Volo.Abp.Modularity;
using Microsoft.Extensions.Caching.Distributed;
using MongoCache;

namespace MongoCache
{
    public class MongoDistributedCacheMongoModule: AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            var mongoDbCacheConnectionString = configuration["MongoDbCache:ConnectionString"];

            if (!mongoDbCacheConnectionString.IsNullOrEmpty())
            {
                context.Services.AddMongoDbCache(options =>
                {
                    var mongoDbCacheDatabaseName = configuration["MongoDbCache:DatabaseName"] ?? "AbpCache";
                    var mongoDbCacheCollectionName = configuration["MongoDbCache:CollectionName"] ?? "appCache";
                    var mongoDbCacheExpiredScanInterval = int.Parse(configuration["MongoDbCache:ExpiredScanInterval"] ?? "10");

                    options.ConnectionString = mongoDbCacheConnectionString;
                    options.DatabaseName = mongoDbCacheDatabaseName;
                    options.CollectionName = mongoDbCacheCollectionName;
                    options.ExpiredScanInterval = TimeSpan.FromMinutes(mongoDbCacheExpiredScanInterval);
                });

                context.Services.Replace(ServiceDescriptor.Singleton<IDistributedCache, MongoDistributedCache>());
            }
        }
    }
}

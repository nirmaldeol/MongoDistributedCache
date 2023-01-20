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

            var mongoDbCacheEnabled = configuration["MongoDbCache:IsEnabled"];
            if (mongoDbCacheEnabled.IsNullOrEmpty() || bool.Parse(mongoDbCacheEnabled))
            {
                context.Services.AddMongoDbCache(options =>
                {
                    var mongoDbCacheConnectionString = configuration["MongoDbCache:ConnectionString"];
                    var mongoDbCacheDatabaseName = configuration["MongoDbCache:DatabaseName"] ?? "MongoCache";
                    var mongoDbCacheCollectionName = configuration["MongoDbCache:CollectionName"] ?? "appcache";
                    var mongoDbCacheExpiredScanInterval = int.Parse(configuration["MongoDbCache:ExpiredScanInterval"] ?? "10");
                    if (!mongoDbCacheConnectionString.IsNullOrEmpty())
                    {
                        options.ConnectionString = mongoDbCacheConnectionString;
                        options.DatabaseName = mongoDbCacheDatabaseName;
                        options.CollectionName = mongoDbCacheCollectionName;
                        options.ExpiredScanInterval = TimeSpan.FromMinutes(mongoDbCacheExpiredScanInterval);
                    }
                });

                context.Services.Replace(ServiceDescriptor.Singleton<IDistributedCache, CoderTechMongoCache>());
            }
        }
    }
}

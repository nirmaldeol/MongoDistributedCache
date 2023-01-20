using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoCache
{
    public static class MongoDbCacheServicesExtensions
    {
        public static IServiceCollection AddMongoDbCache(this IServiceCollection services, Action<MongoDbCacheOptions> setupAction)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            services.AddOptions();
            services.Configure(setupAction);
            services.Add(ServiceDescriptor.Singleton<IDistributedCache, MongoDbCache>());

            return services;
        }
    }
}

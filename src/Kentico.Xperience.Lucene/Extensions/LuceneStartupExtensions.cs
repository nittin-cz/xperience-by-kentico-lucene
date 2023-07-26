using System;

using Kentico.Xperience.Lucene.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Lucene.Extensions
{
    /// <summary>
    /// Application startup extension methods.
    /// </summary>
    public static class LuceneStartupExtensions
    {
        /// <summary>
        /// Registers the provided <paramref name="indexes"/>
        /// and <paramref name="crawlers"/> with the <see cref="IndexStore"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="indexes">The Lucene indexes to register.</param>
        public static IServiceCollection AddLucene(this IServiceCollection services, IConfiguration configuration, LuceneIndex[] indexes = null)
        {
            if (indexes != null)
            {
                Array.ForEach(indexes, index => IndexStore.Instance.AddIndex(index));
            }
            
            //if (crawlers != null)
            //{
            //    Array.ForEach(crawlers, crawlerId => IndexStore.Instance.AddCrawler(crawlerId));
            //}

            //services.AddHttpClient();
            return services;
        }
    }
}
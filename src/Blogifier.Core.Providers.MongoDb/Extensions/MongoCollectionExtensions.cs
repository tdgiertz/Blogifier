using Blogifier.Shared;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blogifier.Core.Providers.MongoDb
{
    internal static class MongoCollectionExtensions
    {
        public static async Task<List<T>> GetPagedAsync<T, TIntermediate>(this IMongoCollection<T> collection, PagingDescriptor pagingDescriptor, FilterDefinition<T> filter, IEnumerable<Models.SortDefinition<TIntermediate>> sortDefinitions, Func<IAggregateFluent<T>, IAggregateFluent<TIntermediate>> aggregateFunc, Func<IEnumerable<TIntermediate>, List<T>> convert)
        {
            filter ??= Builders<T>.Filter.Empty;

            var dataPipelineStageDefinitions = new List<PipelineStageDefinition<TIntermediate, TIntermediate>>();

            var sortStageDefinition = GetSortDefinition(sortDefinitions);

            if (sortStageDefinition != null)
            {
                dataPipelineStageDefinitions.Add(PipelineStageDefinitionBuilder.Sort(sortStageDefinition));
            }

            dataPipelineStageDefinitions.Add(PipelineStageDefinitionBuilder.Skip<TIntermediate>(pagingDescriptor.Skip));
            dataPipelineStageDefinitions.Add(PipelineStageDefinitionBuilder.Limit<TIntermediate>(pagingDescriptor.PageSize));

            var countFacet = AggregateFacet.Create("count", PipelineDefinition<TIntermediate, AggregateCountResult>.Create(new[]
                {
                    PipelineStageDefinitionBuilder.Count<TIntermediate>()
                }));

            var dataFacet = AggregateFacet.Create("data", PipelineDefinition<TIntermediate, TIntermediate>.Create(dataPipelineStageDefinitions));

            var aggregateFluent = collection.Aggregate().Match(filter);

            var intermediateAggregateFluent = aggregateFunc(aggregateFluent);

            var aggregation = await intermediateAggregateFluent
                .Facet(countFacet, dataFacet)
                .ToListAsync();

            var count = aggregation.First()
                .Facets.First(x => x.Name == "count")
                .Output<AggregateCountResult>()
                ?.FirstOrDefault()
                ?.Count ?? 0;

            var data = aggregation.First()
                .Facets.First(x => x.Name == "data")
                .Output<TIntermediate>();

            pagingDescriptor.SetTotalCount(count);

            return convert(data);
        }

        public static async Task<List<T>> GetPagedAsync<T>(this IMongoCollection<T> collection, PagingDescriptor pagingDescriptor, FilterDefinition<T> filter, IEnumerable<Models.SortDefinition<T>> sortDefinitions, Func<IAggregateFluent<T>, IAggregateFluent<T>> aggregateFunc = null)
        {
            filter ??= Builders<T>.Filter.Empty;

            var dataPipelineStageDefinitions = new List<PipelineStageDefinition<T, T>>();

            var sortStageDefinition = GetSortDefinition(sortDefinitions);

            if (sortStageDefinition != null)
            {
                dataPipelineStageDefinitions.Add(PipelineStageDefinitionBuilder.Sort(sortStageDefinition));
            }

            dataPipelineStageDefinitions.Add(PipelineStageDefinitionBuilder.Skip<T>(pagingDescriptor.Skip));
            dataPipelineStageDefinitions.Add(PipelineStageDefinitionBuilder.Limit<T>(pagingDescriptor.PageSize));

            var countFacet = AggregateFacet.Create("count", PipelineDefinition<T, AggregateCountResult>.Create(new[]
                {
                    PipelineStageDefinitionBuilder.Count<T>()
                }));

            var dataFacet = AggregateFacet.Create("data", PipelineDefinition<T, T>.Create(dataPipelineStageDefinitions));

            var aggregateFluent = collection.Aggregate().Match(filter);

            if (aggregateFunc != null)
            {
                aggregateFluent = aggregateFunc(aggregateFluent);
            }

            var aggregation = await aggregateFluent
                .Facet(countFacet, dataFacet)
                .ToListAsync();

            var count = aggregation.First()
                .Facets.First(x => x.Name == "count")
                .Output<AggregateCountResult>()
                ?.FirstOrDefault()
                ?.Count ?? 0;

            var data = aggregation.First()
                .Facets.First(x => x.Name == "data")
                .Output<T>();

            pagingDescriptor.SetTotalCount(count);

            return data.ToList();
        }

        private static MongoDB.Driver.SortDefinition<T> GetSortDefinition<T>(IEnumerable<Models.SortDefinition<T>> sortDefinitions)
        {
            var builder = Builders<T>.Sort;

            if (sortDefinitions != null && sortDefinitions.Any())
            {
                MongoDB.Driver.SortDefinition<T> definition = null;
                foreach (var sortDefinition in sortDefinitions)
                {
                    if (definition != null)
                    {
                        definition = sortDefinition.IsDescending
                            ? definition.Descending(sortDefinition.Sort)
                            : definition.Ascending(sortDefinition.Sort);
                    }
                    else
                    {
                        definition = sortDefinition.IsDescending
                            ? builder.Descending(sortDefinition.Sort)
                            : builder.Ascending(sortDefinition.Sort);
                    }
                }

                return definition;
            }

            return null;
        }
    }
}

using System;
using Abp.Dependency;
using GraphQL.Conversion;
using GraphQL.Types;
using LotteryDetection.Queries.Container;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Schemas;

public class MainSchema : Schema, ITransientDependency
{
    public MainSchema(IServiceProvider provider) :
        base(provider)
    {
        Query = provider.GetRequiredService<QueryContainer>();
        NameConverter = new CamelCaseNameConverter();
    }
}
using Abp.Dependency;
using GraphQL.Types;
using LotteryDetection.Queries.Container;
using System;
using GraphQL.Conversion;
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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using GraphQL;
using GraphQL.Types;
using LotteryDetection.Authorization;
using LotteryDetection.Authorization.Roles;
using LotteryDetection.Core.Base;
using LotteryDetection.Core.Extensions;
using LotteryDetection.Dto;
using LotteryDetection.Types;
using Microsoft.EntityFrameworkCore;

namespace LotteryDetection.Queries;

public class RoleQuery : LotteryDetectionQueryBase<ListGraphType<RoleType>, List<RoleDto>>
{
    private readonly RoleManager _roleManager;

    public RoleQuery(RoleManager roleManager)
        : base("roles", new Dictionary<string, Type>
            {
                { Args.Id, typeof(IdGraphType) },
                { Args.Name, typeof(StringGraphType) }
            }
        )
    {
        _roleManager = roleManager;
    }

    [AbpAuthorize(AppPermissions.Pages_Administration_Roles)]
    public override async Task<List<RoleDto>> Resolve(IResolveFieldContext context)
    {
        var query = _roleManager.Roles.AsNoTracking();

        context
            .ContainsArgument<int>(Args.Id, id => query = query.Where(r => r.Id == id))
            .ContainsArgument<string>(Args.Name, name => query = query.Where(r => r.Name == name));

        return await ProjectToListAsync<RoleDto>(query);
    }

    public static class Args
    {
        public const string Id = "id";
        public const string Name = "name";
    }
}
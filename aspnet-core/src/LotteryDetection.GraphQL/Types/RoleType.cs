using GraphQL.Types;
using LotteryDetection.Dto;

namespace LotteryDetection.Types;

public class RoleType : ObjectGraphType<RoleDto>
{
    public RoleType()
    {
        Name = "RoleType";

        Field(x => x.Id);
        Field(x => x.IsDefault);
        Field(x => x.IsStatic);
        Field(x => x.Name);
        Field(x => x.DisplayName);
        Field(x => x.CreationTime);
        Field(x => x.CreatorUserId, true);
        Field(x => x.LastModificationTime, true);
        Field(x => x.LastModifierUserId, true);
        Field(x => x.TenantId, true);
    }
}
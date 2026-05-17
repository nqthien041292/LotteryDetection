using System;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LotteryDetection.Web.Swagger;

public class SwaggerNullableParameterFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        if (!parameter.Schema.Nullable &&
            (context.ApiParameterDescription.Type.IsNullable() || !context.ApiParameterDescription.Type.IsValueType))
            parameter.Schema.Nullable = true;
    }
}

public static class TypeExtensions
{
    public static bool IsNullable(this Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}
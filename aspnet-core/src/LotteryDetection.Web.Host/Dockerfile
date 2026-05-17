#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/LotteryDetection.Web.Host/LotteryDetection.Web.Host.csproj", "src/LotteryDetection.Web.Host/"]
COPY ["src/LotteryDetection.Web.Core/LotteryDetection.Web.Core.csproj", "src/LotteryDetection.Web.Core/"]
COPY ["src/LotteryDetection.Application/LotteryDetection.Application.csproj", "src/LotteryDetection.Application/"]
COPY ["src/LotteryDetection.Application.Shared/LotteryDetection.Application.Shared.csproj", "src/LotteryDetection.Application.Shared/"]
COPY ["src/LotteryDetection.Core.Shared/LotteryDetection.Core.Shared.csproj", "src/LotteryDetection.Core.Shared/"]
COPY ["src/LotteryDetection.Core/LotteryDetection.Core.csproj", "src/LotteryDetection.Core/"]
COPY ["src/LotteryDetection.EntityFrameworkCore/LotteryDetection.EntityFrameworkCore.csproj", "src/LotteryDetection.EntityFrameworkCore/"]
COPY ["src/LotteryDetection.GraphQL/LotteryDetection.GraphQL.csproj", "src/LotteryDetection.GraphQL/"]
RUN dotnet restore "src/LotteryDetection.Web.Host/LotteryDetection.Web.Host.csproj"
COPY . .
WORKDIR "/src/src/LotteryDetection.Web.Host"
RUN dotnet build "LotteryDetection.Web.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LotteryDetection.Web.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LotteryDetection.Web.Host.dll"]

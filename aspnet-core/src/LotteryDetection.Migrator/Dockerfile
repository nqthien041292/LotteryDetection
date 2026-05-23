#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/LotteryDetection.Migrator/LotteryDetection.Migrator.csproj", "src/LotteryDetection.Migrator/"]
COPY ["src/LotteryDetection.EntityFrameworkCore/LotteryDetection.EntityFrameworkCore.csproj", "src/LotteryDetection.EntityFrameworkCore/"]
COPY ["src/LotteryDetection.Core/LotteryDetection.Core.csproj", "src/LotteryDetection.Core/"]
COPY ["src/LotteryDetection.Core.Shared/LotteryDetection.Core.Shared.csproj", "src/LotteryDetection.Core.Shared/"]
RUN dotnet restore "src/LotteryDetection.Migrator/LotteryDetection.Migrator.csproj"
COPY . .
WORKDIR "/src/src/LotteryDetection.Migrator"
RUN dotnet build "LotteryDetection.Migrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LotteryDetection.Migrator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LotteryDetection.Migrator.dll", "-s"]

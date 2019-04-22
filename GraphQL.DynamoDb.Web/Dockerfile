FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["GraphQL.DynamoDb.Web/GraphQL.DynamoDb.Web.csproj", "GraphQL.DynamoDb.Web/"]
RUN dotnet restore "GraphQL.DynamoDb.Web/GraphQL.DynamoDb.Web.csproj"
COPY . .
WORKDIR "/src/GraphQL.DynamoDb.Web"
RUN dotnet build "GraphQL.DynamoDb.Web.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "GraphQL.DynamoDb.Web.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GraphQL.DynamoDb.Web.dll"]
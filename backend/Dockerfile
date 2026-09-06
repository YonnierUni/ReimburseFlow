FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["backend/ReimburseFlow.sln", "backend/"]
COPY ["backend/src/ReimburseFlow.Api/ReimburseFlow.Api.csproj", "backend/src/ReimburseFlow.Api/"]
COPY ["backend/src/ReimburseFlow.Application/ReimburseFlow.Application.csproj", "backend/src/ReimburseFlow.Application/"]
COPY ["backend/src/ReimburseFlow.Domain/ReimburseFlow.Domain.csproj", "backend/src/ReimburseFlow.Domain/"]
COPY ["backend/src/ReimburseFlow.Infrastructure/ReimburseFlow.Infrastructure.csproj", "backend/src/ReimburseFlow.Infrastructure/"]
COPY ["backend/tests/ReimburseFlow.Tests/ReimburseFlow.Tests.csproj", "backend/tests/ReimburseFlow.Tests/"]
RUN dotnet restore "backend/ReimburseFlow.sln"

COPY backend/ backend/
RUN dotnet publish "backend/src/ReimburseFlow.Api/ReimburseFlow.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReimburseFlow.Api.dll"]
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TitanDemo.slnx ./
COPY src/TitanDemo.Api/TitanDemo.Api.csproj src/TitanDemo.Api/
COPY tests/TitanDemo.Api.Tests/TitanDemo.Api.Tests.csproj tests/TitanDemo.Api.Tests/
RUN dotnet restore TitanDemo.slnx

COPY . .
RUN dotnet publish src/TitanDemo.Api/TitanDemo.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TitanDemo.Api.dll"]

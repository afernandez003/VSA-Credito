FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Domain/Domain.csproj src/Domain/
COPY src/Core/Core.csproj src/Core/
COPY src/App/App.csproj src/App/
COPY src/Infra/Infra.csproj src/Infra/
COPY src/Api/Api.csproj src/Api/

RUN dotnet restore src/Api/Api.csproj

COPY . .
RUN dotnet publish src/Api/Api.csproj \
    -c Release -o /app/publish --no-restore \
    -p:OpenApiGenerateDocuments=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN useradd -r -s /bin/false creditos

COPY --from=build /app/publish .

USER creditos
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]

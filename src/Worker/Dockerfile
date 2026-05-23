FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Domain/Domain.csproj src/Domain/
COPY src/Core/Core.csproj src/Core/
COPY src/App/App.csproj src/App/
COPY src/Infra/Infra.csproj src/Infra/
COPY src/Worker/Worker.csproj src/Worker/

RUN dotnet restore src/Worker/Worker.csproj

COPY . .
RUN dotnet publish src/Worker/Worker.csproj \
    -c Release -r linux-x64 --self-contained \
    -o /app/publish --no-restore \
    -p:PublishAot=false

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS runtime
WORKDIR /app

RUN useradd -r -s /bin/false creditos

COPY --from=build /app/publish .

USER creditos
ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["./Worker"]

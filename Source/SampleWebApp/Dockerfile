FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY /SampleWebApp/SampleWebApp.csproj ./SampleWebApp/
COPY /VaultSharp.Extensions.Configuration/VaultSharp.Extensions.Configuration.csproj ./VaultSharp.Extensions.Configuration/
RUN dotnet restore ./SampleWebApp/SampleWebApp.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish ./SampleWebApp/SampleWebApp.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "SampleWebApp.dll"]

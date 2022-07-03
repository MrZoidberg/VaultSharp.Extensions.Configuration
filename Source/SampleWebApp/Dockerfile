FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY /Source/SampleWebApp/SampleWebApp.csproj ./Source/SampleWebApp/
COPY /Source/VaultSharp.Extensions.Configuration/VaultSharp.Extensions.Configuration.csproj ./Source/VaultSharp.Extensions.Configuration/
RUN dotnet restore ./Source/SampleWebApp/SampleWebApp.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish ./Source/SampleWebApp/SampleWebApp.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "SampleWebApp.dll"]

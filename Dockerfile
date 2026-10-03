# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for optimal layer caching
COPY ["MediGuide.Domain/MediGuide.Domain.csproj", "MediGuide.Domain/"]
COPY ["MediGuide.Application/MediGuide.Application.csproj", "MediGuide.Application/"]
COPY ["MediGuide.Infrastructure/MediGuide.Infrastructure.csproj", "MediGuide.Infrastructure/"]
COPY ["MediGuide.API/MediGuide.API.csproj", "MediGuide.API/"]

# Restore dependencies
RUN dotnet restore "MediGuide.API/MediGuide.API.csproj"

# Copy all source code
COPY . .

# Build and publish release
WORKDIR "/src/MediGuide.API"
RUN dotnet publish "MediGuide.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose default HTTP port
EXPOSE 8080

ENTRYPOINT ["dotnet", "MediGuide.API.dll"]

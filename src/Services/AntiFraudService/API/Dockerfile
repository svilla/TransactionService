# Use the official .NET SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and csproj files first to optimize layer caching
COPY ["src/Services/AntiFraudService/API/AntiFraudService.Api.csproj", "Services/AntiFraudService/API/"]
COPY ["src/Services/AntiFraudService/Application/AntiFraudService.Application.csproj", "Services/AntiFraudService/Application/"]
COPY ["src/Services/AntiFraudService/Domain/AntiFraudService.Domain.csproj", "Services/AntiFraudService/Domain/"]
COPY ["src/Services/AntiFraudService/Infrastructure/AntiFraudService.Infrastructure.csproj", "Services/AntiFraudService/Infrastructure/"]

# Restore packages
RUN dotnet restore "Services/AntiFraudService/API/AntiFraudService.Api.csproj"

# Copy the rest of the source code
COPY ["src/Services/AntiFraudService", "Services/AntiFraudService"]

# Build the application
WORKDIR "/src/Services/AntiFraudService/API"
RUN dotnet build "AntiFraudService.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "AntiFraudService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expose port 80
EXPOSE 80

ENTRYPOINT ["dotnet", "AntiFraudService.Api.dll"]
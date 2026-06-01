# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["BillarBlackPool/BillarBlackPool.csproj", "BillarBlackPool/"]
RUN dotnet restore "BillarBlackPool/BillarBlackPool.csproj"

# Copy the rest of the source code and publish
COPY . .
WORKDIR "/src/BillarBlackPool"
RUN dotnet publish "BillarBlackPool.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BillarBlackPool.dll"]

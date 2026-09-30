FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CulinaryBlog.slnx", "./"]
COPY ["src/CulinaryBlog.Domain/CulinaryBlog.Domain.csproj", "src/CulinaryBlog.Domain/"]
COPY ["src/CulinaryBlog.Application/CulinaryBlog.Application.csproj", "src/CulinaryBlog.Application/"]
COPY ["src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj", "src/CulinaryBlog.Infrastructure/"]
COPY ["src/CulinaryBlog.API/CulinaryBlog.API.csproj", "src/CulinaryBlog.API/"]
RUN dotnet restore src/CulinaryBlog.API/CulinaryBlog.API.csproj
COPY . .
RUN dotnet publish src/CulinaryBlog.API/CulinaryBlog.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads
EXPOSE 8080
ENTRYPOINT ["dotnet", "CulinaryBlog.API.dll"]
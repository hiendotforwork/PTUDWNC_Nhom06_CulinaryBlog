FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# Prepared by scripts/test-jobs-observability.ps1 using dotnet publish.
COPY artifacts/jobs-api/ .
RUN mkdir -p /app/wwwroot/uploads
EXPOSE 8080
ENTRYPOINT ["dotnet", "CulinaryBlog.API.dll"]

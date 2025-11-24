# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PaperMail.sln ./
COPY src/PaperMail.Core/PaperMail.Core.csproj src/PaperMail.Core/
COPY src/PaperMail.Application/PaperMail.Application.csproj src/PaperMail.Application/
COPY src/PaperMail.Infrastructure/PaperMail.Infrastructure.csproj src/PaperMail.Infrastructure/
COPY src/PaperMail.Web/PaperMail.Web.csproj src/PaperMail.Web/
RUN dotnet restore PaperMail.sln
COPY src/ ./src/
WORKDIR /src/src/PaperMail.Web
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=build /app/publish .
# Use Docker-specific appsettings if present
ENV ASPNETCORE_ENVIRONMENT=Docker
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaperMail.Web.dll"]

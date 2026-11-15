# ایمیج اجرایی میزبان. از ریشهٔ ریپو build می‌شود:
#   docker build -f Platform.Host/Dockerfile -t platform-host .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY Platform.Domain/ ./Platform.Domain/
COPY Platform.Hostlication/ ./Platform.Hostlication/
COPY Platform.Infrastructure/ ./Platform.Infrastructure/
COPY Platform.Host/ ./Platform.Host/
RUN dotnet publish Platform.Host/Platform.Host.csproj -c Release -o /app/publish --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Platform.Host.dll"]

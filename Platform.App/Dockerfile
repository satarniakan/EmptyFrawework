# ایمیج اجرایی میزبان. از ریشهٔ ریپو build می‌شود:
#   docker build -f Platform.App/Dockerfile -t platform-app .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY Platform.Domain/ ./Platform.Domain/
COPY Platform.Application/ ./Platform.Application/
COPY Platform.Infrastructure/ ./Platform.Infrastructure/
COPY Platform.Web/ ./Platform.Web/
COPY Platform.App/ ./Platform.App/
RUN dotnet publish Platform.App/Platform.App.csproj -c Release -o /app/publish --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Platform.App.dll"]

# ایمیج اجرایی میزبان نمونه. از ریشهٔ ریپو build می‌شود:
#   docker build -f samples/Sample.Web/Dockerfile -t sample-web .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/ ./src/
COPY samples/ ./samples/
COPY tests/ ./tests/
RUN dotnet publish samples/Sample.Web/Sample.Web.csproj -c Release -o /app/publish --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Sample.Web.dll"]

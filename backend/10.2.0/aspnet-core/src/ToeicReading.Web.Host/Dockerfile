FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["src/ToeicReading.Web.Host/ToeicReading.Web.Host.csproj", "src/ToeicReading.Web.Host/"]
COPY ["src/ToeicReading.Web.Core/ToeicReading.Web.Core.csproj", "src/ToeicReading.Web.Core/"]
COPY ["src/ToeicReading.Application/ToeicReading.Application.csproj", "src/ToeicReading.Application/"]
COPY ["src/ToeicReading.Core/ToeicReading.Core.csproj", "src/ToeicReading.Core/"]
COPY ["src/ToeicReading.EntityFrameworkCore/ToeicReading.EntityFrameworkCore.csproj", "src/ToeicReading.EntityFrameworkCore/"]
WORKDIR "/src/src/ToeicReading.Web.Host"
RUN dotnet restore 

WORKDIR /src
COPY ["src/ToeicReading.Web.Host", "src/ToeicReading.Web.Host"]
COPY ["src/ToeicReading.Web.Core", "src/ToeicReading.Web.Core"]
COPY ["src/ToeicReading.Application", "src/ToeicReading.Application"]
COPY ["src/ToeicReading.Core", "src/ToeicReading.Core"]
COPY ["src/ToeicReading.EntityFrameworkCore", "src/ToeicReading.EntityFrameworkCore"]
WORKDIR "/src/src/ToeicReading.Web.Host"
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "ToeicReading.Web.Host.dll"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
RUN ln -s ~/.dotnet/tools/libman /usr/local/bin/libman

WORKDIR /src
COPY ["src/ToeicReading.Web.Mvc/ToeicReading.Web.Mvc.csproj", "src/ToeicReading.Web.Mvc/"]
COPY ["src/ToeicReading.Web.Core/ToeicReading.Web.Core.csproj", "src/ToeicReading.Web.Core/"]
COPY ["src/ToeicReading.Application/ToeicReading.Application.csproj", "src/ToeicReading.Application/"]
COPY ["src/ToeicReading.Core/ToeicReading.Core.csproj", "src/ToeicReading.Core/"]
COPY ["src/ToeicReading.EntityFrameworkCore/ToeicReading.EntityFrameworkCore.csproj", "src/ToeicReading.EntityFrameworkCore/"]
WORKDIR "/src/src/ToeicReading.Web.Mvc"
RUN dotnet restore 

WORKDIR /src
COPY ["src/ToeicReading.Web.Mvc", "src/ToeicReading.Web.Mvc"]
COPY ["src/ToeicReading.Web.Core", "src/ToeicReading.Web.Core"]
COPY ["src/ToeicReading.Application", "src/ToeicReading.Application"]
COPY ["src/ToeicReading.Core", "src/ToeicReading.Core"]
COPY ["src/ToeicReading.EntityFrameworkCore", "src/ToeicReading.EntityFrameworkCore"]
WORKDIR "/src/src/ToeicReading.Web.Mvc"
RUN libman restore
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "ToeicReading.Web.Mvc.dll"]

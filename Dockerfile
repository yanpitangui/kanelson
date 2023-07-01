FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["./Directory.Packages.props", "./"]
COPY ["Kanelson/Kanelson.csproj", "Kanelson/"]

RUN dotnet restore "Kanelson/Kanelson.csproj"
COPY . .
WORKDIR "/src/Kanelson"
RUN dotnet build "Kanelson.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kanelson.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kanelson.dll"]

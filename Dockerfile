FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080


# Install cultures (same approach as Alpine SDK image)
RUN apk add --no-cache icu-libs

# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY ["./Directory.Build.props", "./"]
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

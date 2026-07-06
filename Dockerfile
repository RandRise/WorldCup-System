FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WorldCup-System.sln ./
COPY WorldCup-System/WorldCup-System.csproj WorldCup-System/
COPY Core/Core.csproj Core/
COPY Data/Data.csproj Data/

RUN dotnet restore WorldCup-System/WorldCup-System.csproj

COPY . .
RUN dotnet publish WorldCup-System/WorldCup-System.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WorldCup-System.dll"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NeuroNotes.Api/NeuroNotes.Api.csproj", "NeuroNotes.Api/"]
COPY ["NeuroNotes.Application/NeuroNotes.Application.csproj", "NeuroNotes.Application/"]
COPY ["NeuroNotes.Domain/NeuroNotes.Domain.csproj", "NeuroNotes.Domain/"]
COPY ["NeuroNotes.Infrastructure/NeuroNotes.Infrastructure.csproj", "NeuroNotes.Infrastructure/"]

RUN dotnet restore "NeuroNotes.Api/NeuroNotes.Api.csproj"

COPY . .

WORKDIR "/src/NeuroNotes.Api"
RUN dotnet build "NeuroNotes.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NeuroNotes.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update && \
    apt-get install -y ffmpeg && \
    rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NeuroNotes.Api.dll"]
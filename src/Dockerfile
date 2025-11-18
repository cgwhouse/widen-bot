FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80 81

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY . /src
WORKDIR /src
RUN dotnet restore "WidenBot.csproj" && \
  dotnet clean "WidenBot.csproj" -c Release -o /app/build && \
  dotnet build "WidenBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WidenBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WidenBot.dll"]

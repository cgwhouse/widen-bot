FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY WidenBot.csproj .

# Restore as a separate layer so it is cached across code-only changes
RUN dotnet restore "WidenBot.csproj"

COPY . .
RUN dotnet publish "WidenBot.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WidenBot.dll"]

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
# Expose the port the app runs on
EXPOSE 80
EXPOSE 443
# Use the official .NET Core SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
# Copy the project files into the container
COPY ["ComplimentGeneratorAPI.csproj", "./"]
RUN dotnet restore "ComplimentGeneratorAPI.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ComplimentGeneratorAPI.csproj" -c Release -o /app/build

# Build the application
FROM build AS publish
RUN dotnet publish "ComplimentGeneratorAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Define the startup command
ENTRYPOINT ["dotnet", "ComplimentGeneratorAPI.dll"]

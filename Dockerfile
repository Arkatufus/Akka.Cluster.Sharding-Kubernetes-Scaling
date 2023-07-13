FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Akka.Cluster.Sharding.Scaling/Akka.Cluster.Sharding.Scaling.csproj", "Akka.Cluster.Sharding.Scaling/"]
RUN dotnet restore "Akka.Cluster.Sharding.Scaling/Akka.Cluster.Sharding.Scaling.csproj"
COPY . .
WORKDIR "/src/Akka.Cluster.Sharding.Scaling"
RUN dotnet build "Akka.Cluster.Sharding.Scaling.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Akka.Cluster.Sharding.Scaling.csproj" -c Release -o /app/publish /p:UseAppHost=false
RUN dotnet tool install --global pbm 

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# copy .NET Core global tool
COPY --from=publish /root/.dotnet /root/.dotnet/

# Needed because https://stackoverflow.com/questions/51977474/install-dotnet-core-tool-dockerfile
ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["dotnet", "Akka.Cluster.Sharding.Scaling.dll"]

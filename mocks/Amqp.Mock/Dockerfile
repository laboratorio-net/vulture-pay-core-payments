FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["./mocks/Amqp.Mock/Amqp.Mock.csproj", "./"]
RUN dotnet restore "Amqp.Mock.csproj"

COPY ./mocks/Amqp.Mock/ .
RUN dotnet publish "Amqp.Mock.csproj" --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Amqp.Mock.dll"]

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY LoginService/*.csproj ./LoginService/
COPY LoginServiceTests/*.csproj ./Tests/
RUN dotnet restore

# copy everything else and build app
COPY . .
WORKDIR /app/LoginService
RUN dotnet build

FROM build AS testrunner
WORKDIR /app/Tests
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

FROM build AS test
WORKDIR /app/Tests
RUN dotnet test

FROM build AS publish
WORKDIR /app/LoginService
RUN dotnet publish -c Release -o out

# COPY ./LoginService ./
# RUN dotnet publish -c Debug -o compiledOutput

FROM microsoft/dotnet:2.0-runtime AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://*:4201
EXPOSE 4201
COPY --from=publish /app/LoginService/out ./
ENTRYPOINT ["dotnet", "LoginService.dll"]
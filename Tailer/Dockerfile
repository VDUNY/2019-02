# Multistage docker build (don't forget http://aka.ms/containercompat)

# SDK container for building
FROM jaykul/powershell:sdk-alpine AS build
WORKDIR /src

# project file and dependencies in first layer
COPY Tailer/Tailer.csproj Tailer/
RUN dotnet restore Tailer/Tailer.csproj

# source and build in second layer
COPY . .
WORKDIR /src/Tailer
RUN dotnet build Tailer.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Tailer.csproj -c Release -o /app

# Compose our output image
# simple container for output
FROM microsoft/dotnet:2.2-runtime-alpine AS release
WORKDIR /app

LABEL maintainer="Joel Bennett <Jaykul@HuddledMasses.org>" \
    org.label-schema.schema-version="1.0" \
    org.label-schema.name="Tailer" \
	description="W3C file tail and alerts"

COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Tailer.dll"]

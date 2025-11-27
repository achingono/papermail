ARG VARIANT=8.0
ARG PROJECT=Papermail.Web
ARG CONFIG=Release

FROM mcr.microsoft.com/dotnet/sdk:${VARIANT} AS sdk
ARG PROJECT
ARG CONFIG
ENV PROJECT=${PROJECT}

EXPOSE 8080

# Install npm and nodejs
RUN apt-get update && \
    apt-get install -y curl gnupg && \
    curl -fsSL https://deb.nodesource.com/setup_25.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

# Install debugger
RUN curl -sSL https://aka.ms/getvsdbgsh | \
    bash /dev/stdin -v latest -l /vsdbg

WORKDIR /src

FROM mcr.microsoft.com/dotnet/aspnet:${VARIANT} AS base
ARG PROJECT
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM sdk AS build
ARG PROJECT
ARG CONFIG

# copy project files
COPY src/Papermail.Core/Papermail.Core.csproj ./Papermail.Core/
COPY src/Papermail.Data/Papermail.Data.csproj ./Papermail.Data/
COPY src/${PROJECT}/${PROJECT}.csproj ./${PROJECT}/
COPY src/${PROJECT}/package*.json ./${PROJECT}/

# restore dependencies
RUN dotnet restore ./${PROJECT}
RUN npm ci --prefix ./${PROJECT}

FROM build AS publish
ARG PROJECT
ARG CONFIG
COPY src/ .
RUN dotnet publish ./${PROJECT} -c ${CONFIG} -o /app/publish

FROM base AS final
ARG PROJECT
ENV ASSEMBLY=${PROJECT}.dll

COPY --from=sdk /vsdbg /vsdbg

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet $ASSEMBLY"]
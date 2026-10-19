# Build the pinned official client source without pulling from Quay.
FROM golang:1.24 AS build
WORKDIR /src
ADD https://github.com/minio/mc/archive/refs/tags/RELEASE.2025-04-16T18-13-26Z.tar.gz /tmp/mc.tar.gz
RUN tar -xzf /tmp/mc.tar.gz -C /src --strip-components=1
RUN CGO_ENABLED=0 go build -trimpath -o /out/mc .
FROM mcr.microsoft.com/dotnet/aspnet:10.0
COPY --from=build /out/mc /usr/local/bin/mc
ENTRYPOINT ["mc"]

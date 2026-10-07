# Build the pinned official community source when public binary registries deny access.
FROM golang:1.24 AS build
WORKDIR /src
ADD https://github.com/minio/minio/archive/refs/tags/RELEASE.2025-04-22T22-12-26Z.tar.gz /tmp/minio.tar.gz
RUN tar -xzf /tmp/minio.tar.gz -C /src --strip-components=1
RUN CGO_ENABLED=0 go build -trimpath -o /out/minio .
FROM mcr.microsoft.com/dotnet/aspnet:10.0
COPY --from=build /out/minio /usr/local/bin/minio
EXPOSE 9000 9001
ENTRYPOINT ["minio"]
CMD ["server", "/data", "--console-address", ":9001"]

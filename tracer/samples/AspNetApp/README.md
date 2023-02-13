# ASP.NET Core Docker Sample
This directory creates images that inherit from the [.NET Samples - AspNetApp](https://hub.docker.com/_/microsoft-dotnet-samples) official images and install the .NET Tracer inside the container. To run each image, follow the below instructions.

## Debian amd64
Run the application and expose the website on http://localhost:8000

```console
docker build -f .\Dockerfile.debian-amd64 -t aspnetapp_debian_amd64 .
docker run -it --rm -p 8000:80 aspnetapp_debian_amd64
```

## Debian arm64
Run the application and expose the website on http://localhost:8000

```console
docker build -f .\Dockerfile.debian-arm64 -t aspnetapp_debian_arm64 .
docker run -it --rm -p 8000:80 aspnetapp_debian_arm64
```

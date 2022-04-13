FROM mcr.microsoft.com/dotnet/aspnet
WORKDIR /app

COPY ./ .

RUN mkdir /cube
VOLUME /cube
ENV BasePath /cube

EXPOSE 80

ENTRYPOINT ["dotnet", "CubeSSO.dll"]

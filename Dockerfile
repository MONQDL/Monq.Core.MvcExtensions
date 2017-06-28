FROM microsoft/dotnet:1.1.0-sdk-projectjson

RUN apt-get update && apt-get install -y \
locales \
nuget

RUN localedef -i ru_RU -c -f UTF-8 -A /usr/share/locale/locale.alias ru_RU.UTF-8
ENV LANG="ru_RU.UTF-8"
ENV LC_ALL="ru_RU.UTF-8"
ENV LANGUAGE="ru_RU:ru"

#timezone
RUN echo 'Europe/Moscow' > /etc/timezone && dpkg-reconfigure -f noninteractive tzdata

ENV STARTUP_PROJECT src/Monq.Tools.MvcExtensions
ENV STARTUP_PROJECT_TEST src/Monq.Tools.MvcExtensions.Tests
 
COPY global.json /app/
COPY NuGet.config /app/
 
COPY $STARTUP_PROJECT/project.json /app/$STARTUP_PROJECT/
COPY $STARTUP_PROJECT_TEST/project.json  /app/$STARTUP_PROJECT_TEST/

WORKDIR /app/$STARTUP_PROJECT_TEST
RUN ["dotnet", "restore"] 
 
WORKDIR /app/$STARTUP_PROJECT
RUN ["dotnet", "restore"]
 
COPY . /app


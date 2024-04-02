### Development postponed indefinitely due to [annihilation of upstream projects](https://wiki.mrxiaom.top/mirai/sign.html).

# Exomemory

_'Blink, in retrospect._

Icalingua++ search engine

## Story

> 2022-11-XX

> May I ask, what are you doing with decrypting the QQ database?

> You should know, that in the last two years, QQ no longer displays chat histories from groups that you've left in the message manager, but they still exist.  
I've always used the 2018 version of TIM, until the first half of this year when my 2018 TIM was "phased out", remotely and silently - they didn't ban me from using this old version, just one day I surprisingly found that I couldn't view any chat histories any more, but I could still export them.  
So I need to find a way to view these chat histories.

And eventually it became something different.

## Prerequisites

An Icalingua++ format chat history database in PostgreSQL with a full text search configuration of Chinese language (e.g. zhparser)

Elasticsearch and/or other databases are not planned to be supported shortly in this version of Exomemory.

## Quick start

HTTPS is highly recommended - transferring your chat history in plaintext ~~is absolutely insane~~ is not a good idea. You can use a reverse proxy like Nginx to enable HTTPS if deploying with Docker.

### Docker

- Download src/Exomemory/appsettings.json and fill in the values
- ~~```docker run -d -p 127.0.0.1:5000:5000 -v /path/to/appsettings.json:/app/appsettings.json ghcr.io/CatMe0w/Exomemory```~~
- Deploy or configure your reverse proxy like Nginx to enable HTTPS and expose the application to the Internet
- Open https://exomemory.catme0w.org, enter your Exomemory server URL, your username and password

> Your chat history won't be sent to us. Learn more about the dashboard application: https://github.com/CatMe0w/exomemory-dashboard

### Without Docker

- Clone the repository
- Edit the src/Exomemory/appsettings.json file and fill in the values
- Read below sections to build and run the application
- Open ~~https://exomemory.catme0w.org~~, enter your Exomemory server URL, your username and password

## Build and test the application

### Windows

Run the `build.bat` script in order to restore, build and test the application:

```
> ./build.bat
```

### Linux/macOS

Run the `build.sh` script in order to restore, build and test the application:

```
$ ./build.sh
```

## Run the application

After a successful build you can start the web application by executing the following command in your terminal:

```
dotnet run --project src/Exomemory
```

The application uses HTTPS redirection when run in production which is the default unless explicitly overridden. If you
don't have a certificate configured for HTTPS, be sure to set `ASPNETCORE_ENVIRONMENT=Development`. In order to test
production mode during development you can generate a self signed certificate using this
guide: https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide

After the application has started visit [http://localhost:5000](http://localhost:5000) in your preferred browser.

## Name

The name, Exomemory, comes from the science fiction novel The Quantum Thief by Hannu Rajaniemi.

## One more thing

In the beginning, the project was just a prototype for my own use.

So you may find it's not very user-friendly, especially for the unusual "icalingua + postgres + zhparser" requirement.

Stay tuned for Exomemory 2.

## License

MIT License

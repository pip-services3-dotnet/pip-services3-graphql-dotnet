# <img src="https://uploads-ssl.webflow.com/5ea5d3315186cf5ec60c3ee4/5edf1c94ce4c859f2b188094_logo.svg" alt="Pip.Services Logo" width="200"> <br/> GraphQL for Pip.Services in .NET

This module is a part of the [Pip.Services](http://pipservices.org) polyglot microservices toolkit.

The rpc module provides the synchronous communication using local calls or the HTTP(S) protocol. It contains both server and client side implementations.

The module contains the following packages:
- **Build** - GraphQL service factory
- **Clients** - mechanisms for retrieving connection settings from the microservice’s configuration and providing clients and services with these settings
- **Services** - basic implementation of services for connecting via the HTTP/GraphQL protocol

<a name="links"></a> Quick links:

* [Your first microservice in .Net](https://www.pipservices.org/docs/quickstart/net) 
* [Data Microservice. Step 5](https://www.pipservices.org/docs/tutorials/data-microservice/service)
* [Microservice Facade](https://www.pipservices.org/docs/tutorials/microservice-facade/microservice-facade-main) 
* [Client Library. Step 2](https://www.pipservices.org/docs/tutorials/client-lib/direct-client)
* [Client Library. Step 3](https://www.pipservices.org/docs/tutorials/client-lib/http-client)
* [API Reference](https://pip-services3-dotnet.github.io/pip-services3-rpc-dotnet)
* [Change Log](CHANGELOG.md)
* [Get Help](https://www.pipservices.org/community/help)
* [Contribute](https://www.pipservices.org/community/contribute)

## Use

Install the dotnet package as
```bash
dotnet add package PipServices3.GraphQL
```

## Develop

For development you shall install the following prerequisites:
* Core .NET SDK 3.1+
* Visual Studio Code or another IDE of your choice
* Docker

Restore dependencies:
```bash
dotnet restore src/src.csproj
```

Compile the code:
```bash
dotnet build src/src.csproj
```

Run automated tests:
```bash
dotnet restore test/test.csproj
dotnet test test/test.csproj
```

Generate API documentation:
```bash
./docgen.ps1
```

Before committing changes run dockerized build and test as:
```bash
./build.ps1
./test.ps1
./clear.ps1
```

## Contacts

The .NET version of Pip.Services is created and maintained by:
- **Alex Mazur**
- **Mark Zontak**
- **Sergey Seroukhov**
- **Volodymyr Tkachenko**

Many thanks to contibutors, who put their time and talant into making this project better:
- **Nick Jimenez**, BootBarn Inc.

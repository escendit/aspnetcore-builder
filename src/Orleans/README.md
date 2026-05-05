# Escendit.AspNetCore.Builder.Orleans

This package provides extensions for `WebApplicationBuilder` to configure Orleans clients in ASP.NET Core applications.

## Installation

```bash
dotnet add package Escendit.AspNetCore.Builder.Orleans
```

## Usage

### Orleans Client Runtime

Use `AddOrleansClientRuntime` to configure default hosting, clustering, and activity propagation for the Orleans client.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddOrleansClientRuntime();
```

### Clustering

You can configure specific clustering providers:

#### ADO.NET Clustering

```csharp
builder.AddAdoNetClientClustering(
    connectionStringName: "Clustering:AdoNet",
    connectionStringInvariant: "Npgsql");
```

#### Redis Clustering

```csharp
builder.AddRedisClientClustering(connectionStringName: "Clustering:Redis");
```

### Streaming

#### NATS Streaming

```csharp
builder.AddNatsClientStreams(
    name: "Platform",
    connectionStringName: "Streaming:NATS");
```

### Hosting Defaults

`AddClientHostingDefaults` configures `ClusterConnectionStatusProvider`, connection retry filters, and binds `ClusterOptions`.

```csharp
builder.AddClientHostingDefaults();
```

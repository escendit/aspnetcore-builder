# Escendit.AspNetCore.Builder.Core

This package provides core extensions for `WebApplication` to simplify common configuration tasks.

## Installation

```bash
dotnet add package Escendit.AspNetCore.Builder.Core
```

## Usage

### Gateway API Mapping

Use `MapGatewayApi` to configure the base path for your API. It can take an optional mount point or read it from configuration (`DOTNET_GATEWAY_MOUNTPOINT`).

```csharp
var app = builder.Build();
app.MapGatewayApi("/api/v1");
```

### Exception Handling

Use `UseExceptionHandling` to configure a standardized exception handler that returns `ProblemDetails` for `HttpRequestException` and other exceptions.

```csharp
var app = builder.Build();
app.UseExceptionHandling();
```

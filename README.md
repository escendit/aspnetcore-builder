# ASP.NET Core Builder Extensions

ASP.NET Core Builder Extensions provides a set of extensions for `WebApplicationBuilder` to simplify the configuration of various services and integrations within Escendit projects.

## Installation

This is a base library or repository for ASP.NET Core builder-related extensions. Specific implementations can be found in their respective repositories.

To install the core package (if applicable):

```bash
dotnet add package Escendit.AspNetCore.Builder
```

## Usage

These extensions are typically used during the application startup to configure services:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Example usage of an extension (from a specific implementation)
// builder.AddCustomService(...);

var app = builder.Build();
app.Run();
```

## Contributing

If you'd like to contribute, please fork the repository and make changes as you'd like. Pull requests are warmly welcome.

## License

This project is licensed under the Apache License 2.0.

# SharedLibrary

A shared .NET library containing common models and DTOs for the Cine.NET project.

## Structure

```
SharedLibrary/
├── Domain/
│   └── Entities/          # Domain entities
└── DTOs/
    ├── Requests/          # Request DTOs for API calls
    └── Responses/         # Response DTOs for API calls
```

## Usage

This library is shared between the API and WA (Web Application) projects to ensure consistent data structures.

### Adding Reference

Add the following reference to your `.csproj` file:

```xml
<ItemGroup>
  <ProjectReference Include="..\SharedLibrary\SharedLibrary.csproj" />
</ItemGroup>
```

## Technische Details

- **Framework:** .NET 10.0
- **Nullable:** Enabled
- **Implicit Usings:** Enabled



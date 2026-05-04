# Cine.NET Web Assembly (WA)

A Blazor WebAssembly application built with .NET 10.0 for cinema management.

## Project Overview

Cine.NET_WA is a client-side Blazor WebAssembly application that provides a web interface for cinema management. The application uses a clean architecture with separation of concerns across API clients, repositories, services, and UI components.

## Technology Stack

- **.NET 10.0** - Target Framework
- **Blazor WebAssembly** - Client-side web framework
- **MudBlazor 8.15.0** - Material Design component library
- **ASP.NET Core Components** - Component-based UI framework
- **Docker** - Containerization support

## Project Structure

```
WA/
├── ApiClients/                    # API client implementations
├── Auth/                   # Authentication state management
├── Layout/                 # Layout components
├── Pages/                  # Razor page components
├── Properties/             # Launch settings
└── wwwroot/                # Static assets
```

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (optional, for containerized deployment)

### Running Locally

1. **Run the application**

Clean the project first:
```bash
dotnet clean
```

Then run the application:
```bash
   dotnet run
   ```

**Open your browser**
   - Navigate to `http://localhost:5031`
   - HTTPS: `https://localhost:7169`

### Running with Docker

1. **Build and run using Docker Compose**
   ```bash
   docker-compose up --build
   ```

2. **Access the application**
   - Navigate to `http://localhost:5031`

## Testing 1-2-3
### API Environment Configuration

The Blazor WebAssembly client is configured to use the production API by default to ensure stable and predictable behavior in deployments.

The API base URL is defined in the Blazor WebAssembly startup file:

```
WA/Program.cs
```

By default, the HttpClient is configured with:

```
https://p3api-prod.gielvangaal.dev/
```

#### Switching to ACC (Testing)

To test against the ACC environment, temporarily update the API BaseAddress in the Blazor WebAssembly `Program.cs` file:

```csharp
const string ApiBaseUrl = "https://p3api-acc.gielvangaal.dev/";
```

After testing, revert the BaseAddress back to the production URL.

> Note: The API URL is intentionally hardcoded in the WA `Program.cs` to keep the default configuration aligned with production and avoid accidental environment mismatches during development and deployment.


## Key Features

- **User Management** - View and manage users
- **Authentication** - Built-in authentication state management
- **Responsive UI** - Material Design with MudBlazor components
- **Clean Architecture** - Separated concerns with repositories, services, and API layers

## Configuration

The application connects to a backend API:
- **API Base URL**: `https://p3api-prod.gielvangaal.dev/`

Update the API endpoint in `Program.cs` if needed:
```csharp
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("https://p3api-prod.gielvangaal.dev/") });
```

## Available Pages

- `/` - Home page
- `/counter` - Counter demo page
- `/weather` - Weather forecast demo
- `/users` - User management page

## Architecture Layers

### API Layer (`Api/`)
- Handles HTTP communication with backend services
- Implements API client interfaces

## Authentication

The application uses a custom `AuthStateProvider` for managing authentication state. Authentication is configured with ASP.NET Core's built-in authorization system.

## Docker Support

The application includes Docker support with:
- **Dockerfile** - Container configuration
- **docker-compose.yaml** - Multi-container orchestration
- **Port 5031** - Default application port

## Development

### Build the project
```bash
dotnet build
```

### Run in watch mode
```bash
dotnet watch run
```

## Notes

- This is a WebAssembly application, running entirely in the browser
- The application requires an active API backend to function properly
- All API calls are made to the configured backend endpoint

## Project Information

- **Project Type**: School Project (P3)
- **Framework**: Blazor WebAssembly
- **Target**: .NET 10.0

## Support

For issues or questions, please refer to the project documentation or contact the development team.


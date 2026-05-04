```
██████╗██╗███╗   ██╗███████╗    ███╗   ██╗███████╗████████╗
██╔(__) ██║████╗  ██║██╔════╝    ████╗  ██║██╔════╝╚══██╔══╝
██║     ██║██╔██╗ ██║█████╗      ██╔██╗ ██║█████╗     ██║
██║     ██║██║╚██╗██║██╔══╝      ██║╚██╗██║██╔══╝     ██║
╚██████╗██║██║ ╚████║███████╗ ██ ██║ ╚████║███████╗   ██║
 ╚═════╝╚═╝╚═╝  ╚═══╝╚══════╝ ═╝ ╚═╝  ╚═══╝╚══════╝   ╚═╝

        Team: Fauve, Richard, Giel, Ivar, Ruben, Yoran, Bart
```

# FitLife API

This is the API for the FitLife app.

The focus is on architecture, data flow, and correct ORM usage. We're working Agile/Scrum with the motto: 
*"When you see the future, you know what you've got to do."*

- Clear separation of concerns through a layered architecture
- Entity Framework Core 9 as ORM
- MySQL 8.4 as persistent storage
- Docker Compose for containerized local development
- Swagger/OpenAPI for API documentation
- Health checks for monitoring

## Architecture

The application follows a classic layered architecture:

```
Database (MySQL) 
    ↓ (via EntityFrameworkCore)
Infrastructure (AppDbContext)
    ↓
Repository Layer (IUserRepository)
    ↓
Service Layer (IUserService)
    ↓
Controller Layer (HTTP Endpoints)
    ↓
Client
```

### Responsibilities per layer

**Database (MySQL)**  
Persistent storage, running via Docker. Configured for initialization with seed data.

**Infrastructure (AppDbContext)**  
EF Core DbContext that maps database tables to C# domain models.

**Repository layer**  
Data access using EF Core. No business logic. Provides async methods for querying.

**Service layer**  
Business logic and orchestration. Implements use cases and business rules.

**Controller layer**  
HTTP endpoints, validation, and API contract. Returns DTOs to clients.

**Mappers**  
Convert between Domain Entities and DTOs (Data Transfer Objects).

### Model choice

**Domain model = EF Core entity**

The domain model is used directly by EF Core via the Repository and Service layers.  
This avoids unnecessary mapping in data access and is a pragmatic, common choice for CRUD-driven applications.

DTOs are used for API responses to decouple the API contract from the domain model.

## Project Structure

```
src/
  Controllers/           # HTTP endpoints
    UsersController.cs
  Domain/               # Domain entities
    Entities/
      User.cs
  DTOs/                 # Data transfer objects
    Responses/
      UserResponse.cs
  Infrastructure/       # EF Core DbContext
    Database/
      AppDbContext.cs
  Mappers/              # Entity ↔ DTO mapping
    UserMapper.cs
  Repositories/         # Data access layer
    Interfaces/
      IUserRepository.cs
    Implementations/
      UserRepository.cs
  Services/             # Business logic layer
    Interfaces/
      IUserService.cs
    Implementations/
      UserService.cs
Program.cs              # Application entry point and DI configuration
appsettings.json        # Logging configuration
```

## Configuration

The application reads configuration via environment variables at runtime.

### Environment Variables (Docker)

This project uses a `.env` file for Docker Compose configuration and runtime application settings.

**Setup:**
1. Copy `.env.example` to `.env`
2. Fill in the required credentials and configuration values
3. The `.env` file is ignored by Git for security

#### Configuration Reference

The following variables are supported and documented in `.env.example`:

| Category | Variable | Description |
| :--- | :--- | :--- |
| **Environment** | `ASPNETCORE_ENVIRONMENT` | Defines the runtime environment (e.g., `Development`, `Production`). |
| **Database** | `DB_HOST` | Hostname of the MySQL database container (usually `db`). |
| | `DB_PORT` | Port number for the MySQL database (default: `3306`). |
| | `DB_NAME` | Name of the database to be created/used. |
| | `DB_USER` | The application's database user. |
| | `DB_PASSWORD` | Password for the application's database user. |
| **Root Access** | `DB_ROOT_USER` | Root user for MySQL (required for initialization and phpMyAdmin). |
| | `DB_ROOT_PASSWORD` | Root password for MySQL. |
| **Tooling** | `PHP_MYADMIN_HOST` | Hostname for phpMyAdmin connection (usually `db`). |
| **External APIs** | `TMDB_API_KEY_READ_ONLY` | Your API key for The Movie Database (TMDB). |
| **Email Server** | `MAIL_SERVER_URL` | Hostname of the SMTP server (e.g., `localhost`). |
| | `MAIL_SERVER_PORT` | Port number of the SMTP server (e.g., `25`, `587`). |
| | `MAIL_SENDER_EMAIL` | The email address shown as the sender. |
| | `MAIL_SENDER_USERNAME` | Username for SMTP authentication (optional for local). |
| | `MAIL_SENDER_PASSWORD` | Password for SMTP authentication (optional for local). |

### Email Server configuration

For local development and testing of email functionality, you need to configure an SMTP server.

**Recommended Local Testing Tool:**
We suggest using **[Papercut SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP)** for local testing. It acts as a dummy SMTP server that catches all outgoing emails and displays them in a UI without actually sending them to real addresses.

1. Download and run Papercut SMTP.
2. Set `MAIL_SERVER_URL=localhost` and `MAIL_SERVER_PORT=25` (or your configured port) in your `.env`.
3. No authentication is usually required for Papercut.

## Running the Application

### Prerequisites

- Docker and Docker Compose installed
- `.env` file configured (copy from `.env.example`)

### Local Development with Docker Compose

Start the full stack:

```bash
docker compose up
```

This will:
- Start MySQL 8.4 on port 3306
- Build and start the ASP.NET Core API on port 8080
- Start phpMyAdmin on port 8081 (for database inspection)
- Initialize the database with seed data if available

The API will be available at: **http://localhost:8080**

Be aware! 8080 will not show anything, you'll have to navigate to a endpoint to
see the response. For example: **http://localhost:8080/api/users**


Start full stack in detached mode:
```
docker compose up -d
```

### Rebuild Docker Image

If you modify the application code:

```bash
docker compose up --build
```

Or use the provided PowerShell script:

```powershell
.\rebuild.docker.ps1
```

### Stop the Stack

```bash
docker compose down
```

To also remove database volumes (clean slate):

```bash
docker compose down -v
```

## API Endpoints

All endpoints are async and return JSON. Base URL: `http://localhost:8080`

### Health Check

**Application Health**
- **Method:** `GET`
- **Path:** `/health`
- **Response:** `200 OK` when healthy

### API Documentation

#### Docker Build Context

The Docker build context is set to the repository root instead of the `API/` directory.

This is required because the solution consists of multiple projects:

- `API/`
- `SharedLibrary/`

When the build context is limited to the `API/` folder, Docker cannot access the `SharedLibrary` project.  
This results in missing project references during `dotnet restore` and `dotnet publish`, which can cause the application (including Swagger) to fail.

To solve this, the Docker Compose configuration uses:

```yaml
build:
  context: ..
  dockerfile: API/Dockerfile
```
  
This ensures that:
- The entire solution is available during build
- Project references resolve correctly
- Swagger and all application features function as expected
- The setup remains consistent across Windows, Linux, CI, and server environments
- CI/CD deployment on Ubuntu server compatiblilty

#### Swagger / OpenAPI
- **URL:** `http://localhost:8080/swagger`
- View all endpoints, their parameters, and response models
- Try endpoints directly from the UI


## Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Runtime | .NET | 10.0 |
| Web Framework | ASP.NET Core | 10.0 |
| ORM | Entity Framework Core | 9.0.* |
| MySQL Connector | Pomelo.EntityFrameworkCore.MySql | 9.0.0 |
| Database | MySQL | 8.4 |
| API Docs | Swashbuckle.AspNetCore | 7.* |
| Containerization | Docker Compose | - |

## Project File

See `WebApi_PocV1.csproj` for dependencies and target framework configuration.

## Deployment

### Jira & Git Integration

This project uses **Jira-Git integration** to link commits and branches with work items.

**Commit Tagging:**
Every commit must include the Jira ticket key in the commit message to link it with the work item:

```bash
git commit -m "CN-9 ReadMe API update Done"
```

Format: `[TICKET-KEY] [Description]`

This automatically updates the Jira ticket with commit references and helps track work across systems.

**Creating Branches from Jira:**
1. Open a Jira ticket (e.g., [CN project](https://avd-dt-fsa-01.atlassian.net/jira/software/c/projects/CN))
2. Click **"Create branch"** in the ticket
3. Select repository and branch type
4. Branch is created automatically and linked to the ticket

**Project URL:**
[Jira - CN Project](https://avd-dt-fsa-01.atlassian.net/jira/software/c/projects/CN)

### Ansible

The project includes Ansible playbooks for deployment to staging and production environments.
For context: this is only done once or twice during initial deployment on server. The text 
below is here for reference only. 

**Structure:**
- `ansible/inventory/` — Host configurations (acc, prod)
- `ansible/roles/` — Deployment roles:
  - `certbot/` — SSL certificate management
  - `nginx/` — Reverse proxy configuration
  - `webapi/` — Application deployment

**Deploy to ACC (Staging):**
```bash
ansible-playbook -i ansible/inventory/acc.ini ansible/site.yml
```

**Deploy to Production:**
```bash
ansible-playbook -i ansible/inventory/prod.ini ansible/site.yml
```

### GitHub Actions

Automated CI/CD workflows handle testing and deployment:

**`.github/workflows/`** contains:
- `build.yml` — Build and test the application
- `deploy-acc.yml` — Deploy to staging on PR merge
- `deploy-prod.yml` — Deploy to production on release tag

Workflows automatically trigger builds, run tests, and invoke Ansible playbooks.

## Logging

Structured logging is configured in `appsettings.json`:
- **Default Level:** `Information`
- **ASP.NET Core:** `Warning`

Logging configuration for distributed systems is available in `logging/` directory.

**Future Enhancement:**
Plans to implement centralized logging stack (Loki + Grafana) for production monitoring and troubleshooting.

## Notes

- The domain model is used directly by EF Core for simplicity
- All database operations are async to prevent blocking
- DTOs are used for API responses to decouple the contract from the domain
- Health checks provide monitoring endpoint for orchestration systems
- Swagger enables API discovery and testing without external tools
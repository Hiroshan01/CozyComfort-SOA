# Cozy Comfort SOA

ASP.NET Core Web API and lightweight frontend for Cozy Comfort using layered architecture with modules for authentication, blankets, inventory, orders, distributor workflows, manufacturer workflows, and notifications.

## Projects
- `CozyComfort.API` - API entry point, controllers, middleware, Swagger, auth setup
- `CozyComfort.Application` - DTOs, service contracts, business abstractions
- `CozyComfort.Domain` - entities and enums
- `CozyComfort.Infrastructure` - EF Core MySQL persistence, JWT, password hashing, seeded data
- `CozyComfort.Shared` - shared marker assembly

## Default seeded users
- Admin: `admin@cozycomfort.local` / `Admin@123`
- Manufacturer: `manufacturer@cozycomfort.local` / `Manufacturer@123`
- Distributor: `distributor@cozycomfort.local` / `Distributor@123`
- Seller: `seller@cozycomfort.local` / `Seller@123`

Public registration is limited to sellers; admins can promote existing users to other roles through the role management API.

## Configuration
Update `/home/runner/work/CozyComfort-SOA/CozyComfort-SOA/CozyComfort.API/appsettings.json` with your local MySQL connection string and JWT settings before running.

## Run
```bash
dotnet restore /home/runner/work/CozyComfort-SOA/CozyComfort-SOA/CozyComfort.sln
dotnet build /home/runner/work/CozyComfort-SOA/CozyComfort-SOA/CozyComfort.sln
dotnet run --project /home/runner/work/CozyComfort-SOA/CozyComfort-SOA/CozyComfort.API/CozyComfort.API.csproj
```

After the API starts, open `https://localhost:<port>/` to use the frontend dashboard. The frontend is served from `CozyComfort.API/wwwroot` and uses the same-origin API endpoints, so no extra client setup is required.

Swagger is available in development mode at `https://localhost:<port>/swagger`.

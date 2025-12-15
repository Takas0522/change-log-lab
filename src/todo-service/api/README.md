# Todo Service API

## Overview
RESTful API service for managing todo lists and items with role-based access control.

## Features
- **List Management**: Create, read, update, delete todo lists
- **Todo Management**: Full CRUD operations on todos within lists
- **Access Control**: Role-based permissions (owner, editor, viewer)
- **Sharing**: Invite users to collaborate on lists
- **Authentication**: JWT-based authentication with Auth Service
- **Event Sourcing**: Outbox pattern for event-driven architecture (for future realtime sync)

## Architecture
- **Framework**: ASP.NET Core 10.0
- **Database**: PostgreSQL (via Entity Framework Core)
- **Authentication**: JWT tokens from Auth Service
- **API Pattern**: RESTful with nested resources

## API Endpoints

### Lists
- `GET /api/lists` - Get all lists accessible to the user
- `GET /api/lists/{id}` - Get a specific list with todos
- `POST /api/lists` - Create a new list
- `PUT /api/lists/{id}` - Update list (owner/editor only)
- `DELETE /api/lists/{id}` - Delete list (owner only)

### List Members
- `POST /api/lists/{id}/members` - Add member to list (owner only)
- `DELETE /api/lists/{id}/members/{memberId}` - Remove member (owner only)

### Todos
- `GET /api/lists/{listId}/todos` - Get all todos in a list
- `GET /api/lists/{listId}/todos/{id}` - Get a specific todo
- `POST /api/lists/{listId}/todos` - Create a new todo (owner/editor only)
- `PUT /api/lists/{listId}/todos/{id}` - Update todo (owner/editor only)
- `DELETE /api/lists/{listId}/todos/{id}` - Delete todo (owner/editor only)

## Permissions

### Roles
- **owner**: Full access, can manage members
- **editor**: Can read and write lists/todos, cannot manage members
- **viewer**: Read-only access

### Access Rules
- List owner has full control
- Shared members have permissions based on their role
- Viewers can only read lists and todos
- Editors and owners can modify lists and todos

## Setup

### Prerequisites
- .NET 10.0 SDK
- PostgreSQL 14+
- Auth Service running (for JWT validation)

### Configuration
Update `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=todo_db;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "your-secret-key-matching-auth-service",
    "Issuer": "auth-service",
    "Audience": "auth-service"
  }
}
```

### Database Setup
1. Create database:
```bash
cd ../db
psql -U postgres -c "CREATE DATABASE todo_db;"
psql -U postgres -d todo_db -f schema.sql
```

2. Or use Entity Framework migrations (future):
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Running the Service
```bash
dotnet restore
dotnet build
dotnet run
```

The API will be available at `http://localhost:5001` (or as configured in launchSettings.json).

### Development
```bash
# Watch mode for auto-reload
dotnet watch run
```

## Testing

### Using api.http
Create an `api.http` file with sample requests:

```http
### Create a list
POST http://localhost:5001/api/lists
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "title": "My Todo List",
  "description": "Personal tasks"
}

### Get all lists
GET http://localhost:5001/api/lists
Authorization: Bearer YOUR_JWT_TOKEN

### Create a todo
POST http://localhost:5001/api/lists/{listId}/todos
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "title": "Buy groceries",
  "description": "Milk, eggs, bread",
  "dueDate": "2025-12-20T10:00:00Z"
}
```

## Dependencies
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT authentication
- `Microsoft.EntityFrameworkCore` - ORM
- `Npgsql.EntityFrameworkCore.PostgreSQL` - PostgreSQL provider
- `Microsoft.AspNetCore.OpenApi` - OpenAPI/Swagger support

## Future Enhancements (INIT-009/010)
- Outbox event processing for realtime notifications
- Integration with SignalR service for live updates
- Event-driven architecture using PostgreSQL NOTIFY/LISTEN

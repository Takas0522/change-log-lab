# BFF Service API

Backend For Frontend service that aggregates Auth, User, and Todo services.

## Purpose
- Provides a unified API for the Angular frontend
- Handles authentication and authorization
- Proxies requests to backend microservices

## Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/logout` - Logout current device

### User Profile
- `GET /api/users/me` - Get current user profile
- `PUT /api/users/me` - Update user profile
- `GET /api/users/search?q=` - Search users for invitations

### Lists
- `GET /api/lists` - Get all accessible lists
- `GET /api/lists/{id}` - Get list details
- `POST /api/lists` - Create new list
- `PUT /api/lists/{id}` - Update list
- `DELETE /api/lists/{id}` - Delete list
- `POST /api/lists/{id}/invite` - Invite user to list
- `POST /api/lists/{id}/accept` - Accept invitation

### Todos
- `GET /api/lists/{listId}/todos` - Get all todos in list
- `GET /api/lists/{listId}/todos/{id}` - Get todo details
- `POST /api/lists/{listId}/todos` - Create new todo
- `PUT /api/lists/{listId}/todos/{id}` - Update todo
- `DELETE /api/lists/{listId}/todos/{id}` - Delete todo

## Configuration

Configure backend service URLs in `appsettings.json`:
```json
{
  "Services": {
    "AuthService": "http://localhost:5001",
    "UserService": "http://localhost:5002",
    "TodoService": "http://localhost:5003"
  }
}
```

## Running
```bash
dotnet run
```

Runs on http://localhost:5000 by default.

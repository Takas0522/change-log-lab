# User Service API

## Overview
User Service provides user profile management and user search functionality for invitations.

## Dependencies
- Auth Service: JWT validation
- Database: PostgreSQL (user-db)

## API Endpoints

### Profile Management

#### Get Current User Profile
```http
GET /users/me
Authorization: Bearer {jwt_token}
```

#### Update Current User Profile
```http
PUT /users/me
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "displayName": "New Name",
  "bio": "My bio",
  "avatarUrl": "https://example.com/avatar.jpg"
}
```

#### Get User Profile by ID
```http
GET /users/{userId}
Authorization: Bearer {jwt_token}
```

### User Search (for invitations)

#### Search Users
```http
GET /users/search?q={searchTerm}&skip=0&take=20
Authorization: Bearer {jwt_token}
```

### Internal Endpoints

#### Create User Profile
```http
POST /users
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "userId": "00000000-0000-0000-0000-000000000001",
  "email": "user@example.com",
  "displayName": "User Name",
  "bio": "Optional bio",
  "avatarUrl": "https://example.com/avatar.jpg"
}
```

## Configuration

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5435;Database=user_db;Username=user_service;Password=user_password"
  }
}
```

### JWT Settings (must match Auth Service)
```json
{
  "Jwt": {
    "Secret": "same-secret-as-auth-service",
    "Issuer": "auth-service",
    "Audience": "auth-service"
  }
}
```

## Development

### Run the service
```bash
cd src/user-service/api
dotnet restore
dotnet run
```

### Initialize database
```bash
# From db directory
psql -h localhost -p 5435 -U user_service -d user_db -f schema.sql
psql -h localhost -p 5435 -U user_service -d user_db -f seed.sql
```

## Notes
- All endpoints except POST /users require JWT authentication
- User profiles should be created when users register in Auth Service
- Search is case-insensitive and searches both email and display name
- Maximum search results per request: 100

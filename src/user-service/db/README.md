# User Service Database

## Overview
User Service manages user profiles and provides user search functionality for invitations.

## Database: user-db

### Connection
- Host: localhost
- Port: 5435 (or as configured in docker-compose)
- Database: user_db
- User: user_service
- Password: (configured in environment)

## Schema Files
- `schema.sql`: Database schema definition
- `seed.sql`: Development seed data (must align with auth-service users)

## Tables

### user_profiles
User profile information synchronized from Auth Service.

### Migrations
Run schema and seed files in order:
```bash
psql -h localhost -p 5435 -U user_service -d user_db -f schema.sql
psql -h localhost -p 5435 -U user_service -d user_db -f seed.sql
```

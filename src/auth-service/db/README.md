# Auth Service Database

PostgreSQL database for the authentication service.

## Schema

See `schema.sql` for the complete database schema including:

- `users` table - User accounts
- `device_sessions` table - Multi-device session management

## Setup

### Using Docker Compose (recommended)

```bash
docker-compose up -d
```

This will start a PostgreSQL container with:
- Port: 5432
- Database: auth_db
- Username: postgres
- Password: postgres

### Manual Setup

```bash
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE auth_db;

# Connect to the database
\c auth_db

# Apply schema
\i schema.sql

# (Optional) Load seed data
\i seed.sql
```

## Applying Schema

After the database is running, apply the schema:

```bash
psql -U postgres -d auth_db -f schema.sql
```

## Database Migrations

This project currently uses SQL scripts for schema management. Future iterations may include:

- Entity Framework Core Migrations
- Liquibase or Flyway
- Custom migration scripts

## Connection String

```
Host=localhost;Port=5432;Database=auth_db;Username=postgres;Password=postgres
```

**Important:** Change the default credentials in production!

## Tables

### users
Stores user account information with hashed passwords.

### device_sessions
Manages per-device sessions with version tracking for secure logout functionality.

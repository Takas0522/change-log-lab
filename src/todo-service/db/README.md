# Todo Service Database

## Overview
PostgreSQL database for the Todo Service, managing lists, todos, and sharing permissions.

## Database Schema

### Tables

#### lists
- Stores todo lists
- Each list has an owner (user_id from User Service)
- Supports title and description

#### todos
- Individual todo items within a list
- Support completion status, due dates, and ordering
- Cascading delete when parent list is deleted

#### list_members
- Manages list sharing and permissions
- Roles: owner, editor, viewer
- Links lists to users (user_id from User Service)

#### outbox_events
- Event sourcing table for integration with realtime/notification services
- Supports idempotency with event_id
- Triggers NOTIFY on insert for PostgreSQL LISTEN pattern

## Setup

### Prerequisites
- PostgreSQL 14+
- psql command-line tool or database client

### Initial Setup

1. Create database:
```bash
psql -U postgres -c "CREATE DATABASE todo_db;"
```

2. Run schema:
```bash
psql -U postgres -d todo_db -f schema.sql
```

3. (Optional) Run seed data:
```bash
psql -U postgres -d todo_db -f seed.sql
```

### Connection String Format
```
Host=localhost;Port=5432;Database=todo_db;Username=postgres;Password=your_password
```

## Development

### Reset Database
```bash
psql -U postgres -c "DROP DATABASE IF EXISTS todo_db;"
psql -U postgres -c "CREATE DATABASE todo_db;"
psql -U postgres -d todo_db -f schema.sql
```

### Verify Setup
```bash
psql -U postgres -d todo_db -c "\dt"
```

## Notes
- User IDs are stored but not enforced with foreign keys (microservice pattern)
- Event notification uses PostgreSQL NOTIFY/LISTEN for outbox pattern
- All timestamps are stored with timezone (TIMESTAMP WITH TIME ZONE)

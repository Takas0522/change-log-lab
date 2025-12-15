#!/bin/bash
set -e

echo "🔧 Setting up databases..."

# Wait for PostgreSQL to be ready
echo "⏳ Waiting for PostgreSQL to be ready..."
until psql -U postgres -h localhost -c '\q' 2>/dev/null; do
  sleep 1
done
echo "✅ PostgreSQL is ready!"

# Setup auth_db
echo "📦 Setting up auth_db..."
psql -U postgres -h localhost -c "CREATE DATABASE auth_db;" 2>/dev/null || echo "ℹ️  Database auth_db already exists"

echo "📝 Applying auth_db schema..."
psql -U postgres -h localhost -d auth_db -f /workspaces/change-log-lab/src/auth-service/db/schema.sql

echo "🌱 Loading auth_db seed data..."
psql -U postgres -h localhost -d auth_db -f /workspaces/change-log-lab/src/auth-service/db/seed.sql

echo "✅ auth_db setup completed!"

# Setup todo_db
echo "📦 Setting up todo_db..."
psql -U postgres -h localhost -c "CREATE DATABASE todo_db;" 2>/dev/null || echo "ℹ️  Database todo_db already exists"

echo "📝 Applying todo_db schema..."
psql -U postgres -h localhost -d todo_db -f /workspaces/change-log-lab/src/todo-service/db/schema.sql

echo "🌱 Loading todo_db seed data..."
psql -U postgres -h localhost -d todo_db -f /workspaces/change-log-lab/src/todo-service/db/seed.sql

echo "✅ todo_db setup completed!"

# Setup user_db
echo "📦 Setting up user_db..."
psql -U postgres -h localhost -c "CREATE DATABASE user_db;" 2>/dev/null || echo "ℹ️  Database user_db already exists"

echo "📝 Applying user_db schema..."
psql -U postgres -h localhost -d user_db -f /workspaces/change-log-lab/src/user-service/db/schema.sql

echo "🌱 Loading user_db seed data..."
psql -U postgres -h localhost -d user_db -f /workspaces/change-log-lab/src/user-service/db/seed.sql

echo "✅ user_db setup completed!"

echo "🎉 All databases setup completed!"

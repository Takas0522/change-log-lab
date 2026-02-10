#!/bin/bash
set -e

echo "Waiting for PostgreSQL to be ready..."
until psql -U postgres -h localhost -c '\q' 2>/dev/null; do
  sleep 1
done
echo "PostgreSQL is ready."

echo "Creating database..."
psql -U postgres -h localhost -c "CREATE DATABASE sre_agent_lab_db;" 2>/dev/null || echo "Database already exists."

echo "Applying schema..."
psql -U postgres -h localhost -d sre_agent_lab_db -f /workspaces/sre-agent-lab/src/api/db/schema.sql

echo "Loading seed data..."
psql -U postgres -h localhost -d sre_agent_lab_db -f /workspaces/sre-agent-lab/src/api/db/seed.sql

echo "Database setup complete."

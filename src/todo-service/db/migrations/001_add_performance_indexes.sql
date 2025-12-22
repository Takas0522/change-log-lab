-- Todo Service - Performance Optimization Migration
-- Created: 2025-12-19
-- Purpose: Add indexes to improve query performance for date-based filtering

-- Add index on created_at for efficient filtering
-- This index optimizes queries that filter todos by creation date
-- Expected impact: 10x+ performance improvement on large datasets
CREATE INDEX IF NOT EXISTS idx_todos_created_at ON todos(created_at);

-- Optional: Composite index for combined filtering and joining
-- Uncomment if you need to optimize both JOIN and WHERE conditions simultaneously
-- Note: This may overlap with existing idx_todos_list index
-- CREATE INDEX IF NOT EXISTS idx_todos_list_created ON todos(list_id, created_at);

-- Update statistics for query planner
ANALYZE todos;
ANALYZE lists;

-- Verification query: Check if indexes are created
-- Run this after applying the migration:
-- SELECT indexname, indexdef FROM pg_indexes WHERE tablename = 'todos' ORDER BY indexname;

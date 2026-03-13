-- Optimized Query: Monthly Todo Statistics
-- Original issue: Performance review for todo aggregation query
-- Optimization: Unified date column usage, removed unnecessary JOIN

-- OPTION 1: Simplified query (recommended if business logic allows)
-- Uses created_at for both filtering and grouping (consistent logic)
-- Removes unnecessary JOIN since list_id has foreign key constraint
SELECT
    DATE_TRUNC('month', t.created_at) as month,
    COUNT(*) as total_todos,
    COUNT(CASE WHEN t.is_completed THEN 1 END) as completed_todos,
    AVG(EXTRACT(EPOCH FROM (t.updated_at - t.created_at))/86400) as avg_days_to_complete
FROM todos t
WHERE t.created_at >= NOW() - INTERVAL '2 years'
GROUP BY DATE_TRUNC('month', t.created_at)
ORDER BY month DESC;

-- OPTION 2: If you need to group by update month (less common use case)
-- Note: This may include todos created more than 2 years ago but updated recently
SELECT
    DATE_TRUNC('month', t.updated_at) as month,
    COUNT(*) as total_todos,
    COUNT(CASE WHEN t.is_completed THEN 1 END) as completed_todos,
    AVG(EXTRACT(EPOCH FROM (t.updated_at - t.created_at))/86400) as avg_days_to_complete
FROM todos t
WHERE t.updated_at >= NOW() - INTERVAL '2 years'
GROUP BY DATE_TRUNC('month', t.updated_at)
ORDER BY month DESC;

-- OPTION 3: If you need to filter todos that belong to existing lists (with JOIN)
-- Keep this only if you need to exclude orphaned records (unlikely with FK constraints)
SELECT
    DATE_TRUNC('month', t.created_at) as month,
    COUNT(*) as total_todos,
    COUNT(CASE WHEN t.is_completed THEN 1 END) as completed_todos,
    AVG(EXTRACT(EPOCH FROM (t.updated_at - t.created_at))/86400) as avg_days_to_complete
FROM todos t
JOIN lists l ON t.list_id = l.id
WHERE t.created_at >= NOW() - INTERVAL '2 years'
GROUP BY DATE_TRUNC('month', t.created_at)
ORDER BY month DESC;

-- Performance Tips:
-- 1. Ensure idx_todos_created_at index is created (see migration file)
-- 2. Run ANALYZE todos; periodically to update statistics
-- 3. Use OPTION 1 for best performance (no JOIN required)
-- 4. Test with EXPLAIN ANALYZE to verify index usage

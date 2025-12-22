SELECT
    DATE_TRUNC('month', t.updated_at) as month,
    COUNT(*) as total_todos,
    COUNT(CASE WHEN t.is_completed THEN 1 END) as completed_todos,
    AVG(EXTRACT(EPOCH FROM (t.updated_at - t.created_at))/86400) as avg_days_to_complete
FROM todos t
JOIN lists l ON t.list_id = l.id
WHERE t.created_at >= NOW() - INTERVAL '2 years'
GROUP BY DATE_TRUNC('month', t.updated_at)
ORDER BY month DESC;
-- Seed data for auth service (for development only)
-- Password for all users: "password123"
-- BCrypt hash (cost factor 11): $2a$11$vZ9Y5h5YQfJmZ0x5WKQZhO5N8n6TK6X.qL9Hg7aRLKkH5nwY5p5W6

-- Note: In production, never commit real password hashes or use default passwords

INSERT INTO users (id, email, password_hash, display_name, created_at, updated_at) 
VALUES 
    ('00000000-0000-0000-0000-000000000001', 'admin@example.com', '$2a$11$vZ9Y5h5YQfJmZ0x5WKQZhO5N8n6TK6X.qL9Hg7aRLKkH5nwY5p5W6', 'Admin User', NOW(), NOW()),
    ('00000000-0000-0000-0000-000000000002', 'user@example.com', '$2a$11$vZ9Y5h5YQfJmZ0x5WKQZhO5N8n6TK6X.qL9Hg7aRLKkH5nwY5p5W6', 'Regular User', NOW(), NOW()),
    ('00000000-0000-0000-0000-000000000003', 'demo@example.com', '$2a$11$vZ9Y5h5YQfJmZ0x5WKQZhO5N8n6TK6X.qL9Hg7aRLKkH5nwY5p5W6', 'Demo User', NOW(), NOW())
ON CONFLICT (email) DO NOTHING;

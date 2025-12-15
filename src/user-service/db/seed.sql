-- Seed data for user service (for development only)
-- These must align with auth-service seed data

INSERT INTO user_profiles (user_id, email, display_name, bio, avatar_url, created_at, updated_at) 
VALUES 
    ('00000000-0000-0000-0000-000000000001', 'admin@example.com', 'Admin User', 'System administrator', NULL, NOW(), NOW()),
    ('00000000-0000-0000-0000-000000000002', 'user@example.com', 'Regular User', 'Just a regular user', NULL, NOW(), NOW()),
    ('00000000-0000-0000-0000-000000000003', 'demo@example.com', 'Demo User', 'Demo account for testing', NULL, NOW(), NOW())
ON CONFLICT (user_id) DO NOTHING;

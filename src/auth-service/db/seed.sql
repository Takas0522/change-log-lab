-- Seed data for auth service (for development only)
-- Password for all users: "password123"
-- BCrypt hash (cost factor 11): $2b$11$r4NKhBXl3/oxeXzQehqwHO.xU/HUuS6t2IgtNOEq5CGwmgYYMODxq

-- Note: In production, never commit real password hashes or use default passwords

INSERT INTO users (id, email, password_hash, display_name, created_at, updated_at) 
VALUES 
    ('00000000-0000-0000-0000-000000000001', 'admin@example.com', '$2b$11$r4NKhBXl3/oxeXzQehqwHO.xU/HUuS6t2IgtNOEq5CGwmgYYMODxq', 'Admin User', NOW(), NOW()),
    ('00000000-0000-0000-0000-000000000002', 'user@example.com', '$2b$11$r4NKhBXl3/oxeXzQehqwHO.xU/HUuS6t2IgtNOEq5CGwmgYYMODxq', 'Regular User', NOW(), NOW()),
    ('00000000-0000-0000-0000-000000000003', 'demo@example.com', '$2b$11$r4NKhBXl3/oxeXzQehqwHO.xU/HUuS6t2IgtNOEq5CGwmgYYMODxq', 'Demo User', NOW(), NOW())
ON CONFLICT (email) DO NOTHING;

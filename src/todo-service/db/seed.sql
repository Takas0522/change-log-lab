-- Todo Service Seed Data
-- Sample data for development and testing

-- Note: User IDs should correspond to actual users in the Auth/User Service
-- Admin User:   00000000-0000-0000-0000-000000000001 (admin@example.com)
-- Regular User: 00000000-0000-0000-0000-000000000002 (user@example.com)
-- Demo User:    00000000-0000-0000-0000-000000000003 (demo@example.com)

-- Admin's Lists
INSERT INTO lists (id, title, description, owner_id, created_at, updated_at) VALUES
('11111111-1111-1111-1111-111111111111', 'Admin Tasks', 'System administration and management', '00000000-0000-0000-0000-000000000001', NOW(), NOW()),
('22222222-2222-2222-2222-222222222222', 'Team Projects', 'Collaborative work items', '00000000-0000-0000-0000-000000000001', NOW(), NOW()),
('33333333-3333-3333-3333-333333333333', 'Shopping List', 'Things to buy', '00000000-0000-0000-0000-000000000001', NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- Regular User's Lists
INSERT INTO lists (id, title, description, owner_id, created_at, updated_at) VALUES
('44444444-4444-4444-4444-444444444444', 'Personal Tasks', 'My personal todo list', '00000000-0000-0000-0000-000000000002', NOW(), NOW()),
('55555555-5555-5555-5555-555555555555', 'Home Improvements', 'House renovation tasks', '00000000-0000-0000-0000-000000000002', NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- Demo User's Lists
INSERT INTO lists (id, title, description, owner_id, created_at, updated_at) VALUES
('66666666-6666-6666-6666-666666666666', 'Demo Projects', 'Sample projects for demonstration', '00000000-0000-0000-0000-000000000003', NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- Todos for Admin Tasks
INSERT INTO todos (list_id, title, description, is_completed, due_date, position, created_at, updated_at) VALUES
('11111111-1111-1111-1111-111111111111', 'Review system logs', 'Check for any errors or warnings', false, NOW() + INTERVAL '1 day', 0, NOW(), NOW()),
('11111111-1111-1111-1111-111111111111', 'Update security policies', 'Review and update access controls', false, NOW() + INTERVAL '3 days', 1, NOW(), NOW()),
('11111111-1111-1111-1111-111111111111', 'Database backup check', 'Verify backup integrity', true, NOW() - INTERVAL '1 day', 2, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Todos for Team Projects
INSERT INTO todos (list_id, title, description, is_completed, due_date, position, created_at, updated_at) VALUES
('22222222-2222-2222-2222-222222222222', 'Sprint planning meeting', 'Plan next sprint goals', false, NOW() + INTERVAL '2 days', 0, NOW(), NOW()),
('22222222-2222-2222-2222-222222222222', 'Code review', 'Review pull requests from team', false, NOW() + INTERVAL '1 day', 1, NOW(), NOW()),
('22222222-2222-2222-2222-222222222222', 'Deploy to staging', 'Deploy latest changes', true, NOW() - INTERVAL '2 days', 2, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Todos for Shopping List
INSERT INTO todos (list_id, title, description, is_completed, due_date, position, created_at, updated_at) VALUES
('33333333-3333-3333-3333-333333333333', 'Buy groceries', 'Milk, eggs, bread, vegetables', false, NOW() + INTERVAL '1 day', 0, NOW(), NOW()),
('33333333-3333-3333-3333-333333333333', 'Office supplies', 'Printer paper, pens, notebooks', false, NULL, 1, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Todos for Personal Tasks (Regular User)
INSERT INTO todos (list_id, title, description, is_completed, due_date, position, created_at, updated_at) VALUES
('44444444-4444-4444-4444-444444444444', 'Call dentist', 'Schedule annual checkup', false, NOW() + INTERVAL '5 days', 0, NOW(), NOW()),
('44444444-4444-4444-4444-444444444444', 'Read book', 'Finish current chapter', false, NULL, 1, NOW(), NOW()),
('44444444-4444-4444-4444-444444444444', 'Morning exercise', 'Complete daily workout', true, NOW(), 2, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Todos for Home Improvements (Regular User)
INSERT INTO todos (list_id, title, description, is_completed, due_date, position, created_at, updated_at) VALUES
('55555555-5555-5555-5555-555555555555', 'Fix leaky faucet', 'Kitchen sink needs repair', false, NOW() + INTERVAL '7 days', 0, NOW(), NOW()),
('55555555-5555-5555-5555-555555555555', 'Paint bedroom', 'Choose color and buy paint', false, NOW() + INTERVAL '14 days', 1, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Todos for Demo Projects (Demo User)
INSERT INTO todos (list_id, title, description, is_completed, due_date, position, created_at, updated_at) VALUES
('66666666-6666-6666-6666-666666666666', 'Setup demo environment', 'Configure test environment', true, NOW() - INTERVAL '1 day', 0, NOW(), NOW()),
('66666666-6666-6666-6666-666666666666', 'Prepare presentation', 'Create slides for demo', false, NOW() + INTERVAL '3 days', 1, NOW(), NOW()),
('66666666-6666-6666-6666-666666666666', 'Test all features', 'Ensure everything works', false, NOW() + INTERVAL '2 days', 2, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Shared Lists - Regular User has viewer access to Admin's Team Projects
INSERT INTO list_members (list_id, user_id, role, created_at, updated_at) VALUES
('22222222-2222-2222-2222-222222222222', '00000000-0000-0000-0000-000000000002', 'viewer', NOW(), NOW()),
('44444444-4444-4444-4444-444444444444', '00000000-0000-0000-0000-000000000003', 'viewer', NOW(), NOW())
ON CONFLICT (list_id, user_id) DO NOTHING;

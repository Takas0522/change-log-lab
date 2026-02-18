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
INSERT INTO todos (list_id, title, description, is_completed, status, due_date, position, created_at, updated_at) VALUES
('11111111-1111-1111-1111-111111111111', 'Review system logs', 'Check for any errors or warnings', false, 'not_started', NOW() + INTERVAL '1 day', 0, NOW(), NOW()),
('11111111-1111-1111-1111-111111111111', 'Update security policies', 'Review and update access controls', false, 'in_progress', NOW() + INTERVAL '3 days', 1, NOW(), NOW()),
('11111111-1111-1111-1111-111111111111', 'Database backup check', 'Verify backup integrity', true, 'completed', NOW() - INTERVAL '1 day', 2, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Todos for Team Projects
INSERT INTO todos (list_id, title, description, is_completed, status, due_date, position, created_at, updated_at) VALUES
('22222222-2222-2222-2222-222222222222', 'Sprint planning meeting', 'Plan next sprint goals', false, 'not_started', NOW() + INTERVAL '2 days', 0, NOW(), NOW()),
('22222222-2222-2222-2222-222222222222', 'Code review', 'Review pull requests from team', false, 'in_progress', NOW() + INTERVAL '1 day', 1, NOW(), NOW()),
('22222222-2222-2222-2222-222222222222', 'Deploy to staging', 'Deploy latest changes', true, 'completed', NOW() - INTERVAL '2 days', 2, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Todos for Shopping List
INSERT INTO todos (list_id, title, description, is_completed, status, due_date, position, created_at, updated_at) VALUES
('33333333-3333-3333-3333-333333333333', 'Buy groceries', 'Milk, eggs, bread, vegetables', false, 'not_started', NOW() + INTERVAL '1 day', 0, NOW(), NOW()),
('33333333-3333-3333-3333-333333333333', 'Office supplies', 'Printer paper, pens, notebooks', false, 'not_started', NULL, 1, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Todos for Personal Tasks (Regular User)
INSERT INTO todos (list_id, title, description, is_completed, status, due_date, position, created_at, updated_at) VALUES
('44444444-4444-4444-4444-444444444444', 'Call dentist', 'Schedule annual checkup', false, 'not_started', NOW() + INTERVAL '5 days', 0, NOW(), NOW()),
('44444444-4444-4444-4444-444444444444', 'Read book', 'Finish current chapter', false, 'in_progress', NULL, 1, NOW(), NOW()),
('44444444-4444-4444-4444-444444444444', 'Morning exercise', 'Complete daily workout', true, 'completed', NOW(), 2, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Todos for Home Improvements (Regular User)
INSERT INTO todos (list_id, title, description, is_completed, status, due_date, position, created_at, updated_at) VALUES
('55555555-5555-5555-5555-555555555555', 'Fix leaky faucet', 'Kitchen sink needs repair', false, 'in_progress', NOW() + INTERVAL '7 days', 0, NOW(), NOW()),
('55555555-5555-5555-5555-555555555555', 'Paint bedroom', 'Choose color and buy paint', false, 'not_started', NOW() + INTERVAL '14 days', 1, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Todos for Demo Projects (Demo User)
INSERT INTO todos (list_id, title, description, is_completed, status, due_date, position, created_at, updated_at) VALUES
('66666666-6666-6666-6666-666666666666', 'Setup demo environment', 'Configure test environment', true, 'completed', NOW() - INTERVAL '1 day', 0, NOW(), NOW()),
('66666666-6666-6666-6666-666666666666', 'Prepare presentation', 'Create slides for demo', false, 'in_progress', NOW() + INTERVAL '3 days', 1, NOW(), NOW()),
('66666666-6666-6666-6666-666666666666', 'Test all features', 'Ensure everything works', false, 'not_started', NOW() + INTERVAL '2 days', 2, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Labels for Admin Tasks
INSERT INTO labels (id, list_id, name, color, created_at, updated_at) VALUES
('aaaa1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Urgent', '#FF5733', NOW(), NOW()),
('aaaa2222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'Security', '#C70039', NOW(), NOW()),
('aaaa3333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'Maintenance', '#FFC300', NOW(), NOW())
ON CONFLICT (list_id, name) DO NOTHING;

-- Labels for Team Projects
INSERT INTO labels (id, list_id, name, color, created_at, updated_at) VALUES
('bbbb1111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'Frontend', '#3498DB', NOW(), NOW()),
('bbbb2222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'Backend', '#2ECC71', NOW(), NOW()),
('bbbb3333-3333-3333-3333-333333333333', '22222222-2222-2222-2222-222222222222', 'DevOps', '#9B59B6', NOW(), NOW()),
('bbbb4444-4444-4444-4444-444444444444', '22222222-2222-2222-2222-222222222222', 'Bug', '#E74C3C', NOW(), NOW())
ON CONFLICT (list_id, name) DO NOTHING;

-- Labels for Personal Tasks
INSERT INTO labels (id, list_id, name, color, created_at, updated_at) VALUES
('cccc1111-1111-1111-1111-111111111111', '44444444-4444-4444-4444-444444444444', 'Health', '#1ABC9C', NOW(), NOW()),
('cccc2222-2222-2222-2222-222222222222', '44444444-4444-4444-4444-444444444444', 'Learning', '#9B59B6', NOW(), NOW()),
('cccc3333-3333-3333-3333-333333333333', '44444444-4444-4444-4444-444444444444', 'Hobby', '#F39C12', NOW(), NOW())
ON CONFLICT (list_id, name) DO NOTHING;

-- Todo Labels (assign labels to todos)
INSERT INTO todo_labels (todo_id, label_id, created_at) 
SELECT t.id, 'aaaa1111-1111-1111-1111-111111111111', NOW()
FROM todos t WHERE t.title = 'Review system logs'
ON CONFLICT (todo_id, label_id) DO NOTHING;

INSERT INTO todo_labels (todo_id, label_id, created_at)
SELECT t.id, 'aaaa2222-2222-2222-2222-222222222222', NOW()
FROM todos t WHERE t.title = 'Update security policies'
ON CONFLICT (todo_id, label_id) DO NOTHING;

INSERT INTO todo_labels (todo_id, label_id, created_at)
SELECT t.id, 'aaaa1111-1111-1111-1111-111111111111', NOW()
FROM todos t WHERE t.title = 'Update security policies'
ON CONFLICT (todo_id, label_id) DO NOTHING;

INSERT INTO todo_labels (todo_id, label_id, created_at)
SELECT t.id, 'aaaa3333-3333-3333-3333-333333333333', NOW()
FROM todos t WHERE t.title = 'Database backup check'
ON CONFLICT (todo_id, label_id) DO NOTHING;

INSERT INTO todo_labels (todo_id, label_id, created_at)
SELECT t.id, 'bbbb1111-1111-1111-1111-111111111111', NOW()
FROM todos t WHERE t.title = 'Code review'
ON CONFLICT (todo_id, label_id) DO NOTHING;

INSERT INTO todo_labels (todo_id, label_id, created_at)
SELECT t.id, 'bbbb3333-3333-3333-3333-333333333333', NOW()
FROM todos t WHERE t.title = 'Deploy to staging'
ON CONFLICT (todo_id, label_id) DO NOTHING;

INSERT INTO todo_labels (todo_id, label_id, created_at)
SELECT t.id, 'cccc1111-1111-1111-1111-111111111111', NOW()
FROM todos t WHERE t.title = 'Morning exercise'
ON CONFLICT (todo_id, label_id) DO NOTHING;

-- Shared Lists - Regular User has viewer access to Admin's Team Projects
INSERT INTO list_members (list_id, user_id, role, created_at, updated_at) VALUES
('22222222-2222-2222-2222-222222222222', '00000000-0000-0000-0000-000000000002', 'viewer', NOW(), NOW()),
('44444444-4444-4444-4444-444444444444', '00000000-0000-0000-0000-000000000003', 'viewer', NOW(), NOW())
ON CONFLICT (list_id, user_id) DO NOTHING;

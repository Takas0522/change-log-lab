-- Todo Service Seed Data
-- Sample data for development and testing

-- Note: User IDs should correspond to actual users in the User Service
-- Using test user ID: 123e4567-e89b-12d3-a456-426614174000

-- Sample Lists
INSERT INTO lists (id, title, description, owner_id, created_at, updated_at) VALUES
('11111111-1111-1111-1111-111111111111', 'Personal Tasks', 'My personal todo list', '123e4567-e89b-12d3-a456-426614174000', NOW(), NOW()),
('22222222-2222-2222-2222-222222222222', 'Work Projects', 'Work-related tasks', '123e4567-e89b-12d3-a456-426614174000', NOW(), NOW()),
('33333333-3333-3333-3333-333333333333', 'Shopping List', 'Things to buy', '123e4567-e89b-12d3-a456-426614174000', NOW(), NOW());

-- Sample Todos for Personal Tasks
INSERT INTO todos (list_id, title, description, is_completed, due_date, position, created_at, updated_at) VALUES
('11111111-1111-1111-1111-111111111111', 'Buy groceries', 'Milk, eggs, bread', false, NOW() + INTERVAL '2 days', 0, NOW(), NOW()),
('11111111-1111-1111-1111-111111111111', 'Call dentist', 'Schedule appointment', false, NOW() + INTERVAL '1 day', 1, NOW(), NOW()),
('11111111-1111-1111-1111-111111111111', 'Read book', 'Finish chapter 5', true, NULL, 2, NOW(), NOW());

-- Sample Todos for Work Projects
INSERT INTO todos (list_id, title, description, is_completed, due_date, position, created_at, updated_at) VALUES
('22222222-2222-2222-2222-222222222222', 'Complete project proposal', 'Due next week', false, NOW() + INTERVAL '7 days', 0, NOW(), NOW()),
('22222222-2222-2222-2222-222222222222', 'Review code PRs', 'Check team submissions', false, NOW() + INTERVAL '1 day', 1, NOW(), NOW()),
('22222222-2222-2222-2222-222222222222', 'Team meeting', 'Discuss Q1 goals', true, NOW() - INTERVAL '1 day', 2, NOW(), NOW());

-- Sample Todos for Shopping List
INSERT INTO todos (list_id, title, description, is_completed, due_date, position, created_at, updated_at) VALUES
('33333333-3333-3333-3333-333333333333', 'Buy laptop charger', 'USB-C 65W', false, NULL, 0, NOW(), NOW()),
('33333333-3333-3333-3333-333333333333', 'Get birthday gift', 'For Sarah', false, NOW() + INTERVAL '5 days', 1, NOW(), NOW());

-- Sample list member (shared list example)
-- User 456e4567-e89b-12d3-a456-426614174001 is a viewer of Work Projects
INSERT INTO list_members (list_id, user_id, role, created_at, updated_at) VALUES
('22222222-2222-2222-2222-222222222222', '456e4567-e89b-12d3-a456-426614174001', 'viewer', NOW(), NOW());

-- Todo Service Database Schema

-- Lists table
CREATE TABLE IF NOT EXISTS lists (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    owner_id UUID NOT NULL,  -- Reference to User Service user
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_lists_owner ON lists(owner_id);

-- Todos table
CREATE TABLE IF NOT EXISTS todos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    list_id UUID NOT NULL REFERENCES lists(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    status VARCHAR(20) NOT NULL DEFAULT 'not_started',  -- not_started, in_progress, completed, abandoned
    due_date TIMESTAMP WITH TIME ZONE,
    position INT NOT NULL DEFAULT 0,  -- For ordering
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_todos_list ON todos(list_id);
CREATE INDEX IF NOT EXISTS idx_todos_list_position ON todos(list_id, position);
CREATE INDEX IF NOT EXISTS idx_todos_status ON todos(status);

-- List members table for sharing/permission management
CREATE TABLE IF NOT EXISTS list_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    list_id UUID NOT NULL REFERENCES lists(id) ON DELETE CASCADE,
    user_id UUID NOT NULL,  -- Reference to User Service user
    role VARCHAR(50) NOT NULL DEFAULT 'viewer',  -- owner, editor, viewer
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE(list_id, user_id)
);

CREATE INDEX IF NOT EXISTS idx_list_members_list ON list_members(list_id);
CREATE INDEX IF NOT EXISTS idx_list_members_user ON list_members(user_id);
CREATE INDEX IF NOT EXISTS idx_list_members_list_user ON list_members(list_id, user_id);

-- Labels table
CREATE TABLE IF NOT EXISTS labels (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    list_id UUID NOT NULL REFERENCES lists(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    color VARCHAR(7) NOT NULL,  -- HEX color code (e.g., #FF5733)
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE(list_id, name)
);

CREATE INDEX IF NOT EXISTS idx_labels_list ON labels(list_id);

-- Todo labels table (many-to-many relationship)
CREATE TABLE IF NOT EXISTS todo_labels (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    todo_id UUID NOT NULL REFERENCES todos(id) ON DELETE CASCADE,
    label_id UUID NOT NULL REFERENCES labels(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE(todo_id, label_id)
);

CREATE INDEX IF NOT EXISTS idx_todo_labels_todo ON todo_labels(todo_id);
CREATE INDEX IF NOT EXISTS idx_todo_labels_label ON todo_labels(label_id);

-- Outbox table for event-driven architecture (for INIT-009/010)
CREATE TABLE IF NOT EXISTS outbox_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL UNIQUE,  -- For idempotency
    event_type VARCHAR(100) NOT NULL,  -- list_created, todo_updated, etc.
    aggregate_id UUID NOT NULL,  -- List or Todo ID
    aggregate_type VARCHAR(50) NOT NULL,  -- list, todo
    payload JSONB NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMP WITH TIME ZONE
);

CREATE INDEX IF NOT EXISTS idx_outbox_events_event_id ON outbox_events(event_id);
CREATE INDEX IF NOT EXISTS idx_outbox_events_unprocessed ON outbox_events(processed_at) WHERE processed_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_outbox_events_created ON outbox_events(created_at);

-- Function to notify on outbox insert
CREATE OR REPLACE FUNCTION notify_outbox_event() RETURNS TRIGGER AS $$
BEGIN
    PERFORM pg_notify('outbox_events', NEW.event_id::text);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger to send NOTIFY when new event is inserted
DROP TRIGGER IF EXISTS outbox_events_notify ON outbox_events;
CREATE TRIGGER outbox_events_notify
    AFTER INSERT ON outbox_events
    FOR EACH ROW
    EXECUTE FUNCTION notify_outbox_event();

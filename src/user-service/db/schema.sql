-- User Service Database Schema

-- User profiles table
-- Note: user_id references users table in auth-db (microservices pattern: eventual consistency)
CREATE TABLE IF NOT EXISTS user_profiles (
    user_id UUID PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    bio TEXT,
    avatar_url VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_user_profiles_email ON user_profiles(email);
CREATE INDEX IF NOT EXISTS idx_user_profiles_display_name ON user_profiles(display_name);

-- Full-text search index for user search (for invitations)
CREATE INDEX IF NOT EXISTS idx_user_profiles_search ON user_profiles USING gin(
    to_tsvector('english', coalesce(display_name, '') || ' ' || coalesce(email, ''))
);

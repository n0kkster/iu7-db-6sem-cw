CREATE TABLE teams (
    id UUID NOT NULL,
    name TEXT NOT NULL,
    description TEXT
);

CREATE TABLE users (
    id UUID NOT NULL,
    username TEXT NOT NULL,
    email TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    role_id INT NOT NULL,
    team_id UUID NOT NULL
);

CREATE TABLE invites (
    id UUID NOT NULL,
    code TEXT NOT NULL,
    expiration_date TIMESTAMP NOT NULL,
    is_active BOOLEAN NOT NULL,
    team_id UUID NOT NULL
);

CREATE TABLE it_systems (
    id UUID NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    created_at TIMESTAMP NOT NULL,
    team_id UUID NOT NULL
);
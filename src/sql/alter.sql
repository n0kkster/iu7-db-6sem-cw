-- Первичные ключи
ALTER TABLE teams 
    ADD CONSTRAINT pk_teams PRIMARY KEY (id);

ALTER TABLE users 
    ADD CONSTRAINT pk_users PRIMARY KEY (id);

ALTER TABLE invites 
    ADD CONSTRAINT pk_invites PRIMARY KEY (id);

ALTER TABLE it_systems 
    ADD CONSTRAINT pk_it_systems PRIMARY KEY (id);

-- Ограничения уникальности
ALTER TABLE teams 
    ADD CONSTRAINT uq_teams_name UNIQUE (name);

ALTER TABLE users 
    ADD CONSTRAINT uq_users_username UNIQUE (username),
    ADD CONSTRAINT uq_users_email UNIQUE (email);

ALTER TABLE invites 
    ADD CONSTRAINT uq_invites_code UNIQUE (code);

-- Внешние ключи
ALTER TABLE users 
    ADD CONSTRAINT fk_users_team 
        FOREIGN KEY (team_id) REFERENCES teams (id) ON DELETE RESTRICT;

ALTER TABLE invites 
    ADD CONSTRAINT fk_invites_team 
        FOREIGN KEY (team_id) REFERENCES teams (id) ON DELETE CASCADE;

ALTER TABLE it_systems 
    ADD CONSTRAINT fk_it_systems_team 
        FOREIGN KEY (team_id) REFERENCES teams (id) ON DELETE CASCADE;
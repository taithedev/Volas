CREATE TABLE IF NOT EXISTS volas.community_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
 preset text NOT NULL CHECK (preset IN ('aurora','pulse','orbit')),
 created_by uuid NOT NULL REFERENCES volas.licenses(id),
 starts_at timestamptz NOT NULL DEFAULT now(),
 ends_at timestamptz NOT NULL,
 CHECK (ends_at > starts_at)
);
CREATE INDEX IF NOT EXISTS volas_community_events_latest ON volas.community_events(starts_at DESC);
REVOKE ALL ON volas.community_events FROM PUBLIC;

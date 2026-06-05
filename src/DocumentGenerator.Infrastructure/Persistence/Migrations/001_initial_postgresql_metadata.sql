CREATE TABLE IF NOT EXISTS document_jobs (
    id uuid PRIMARY KEY,
    status text NOT NULL,
    template_id text NOT NULL,
    template_version text NOT NULL,
    output_format text NOT NULL,
    payload_hash text NOT NULL,
    idempotency_key text NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    started_at timestamptz NULL,
    completed_at timestamptz NULL,
    failed_at timestamptz NULL,
    failure_reason text NULL,
    claimed_by text NULL,
    claim_expires_at timestamptz NULL,
    retry_count integer NOT NULL DEFAULT 0,
    artifact_id uuid NULL
);

CREATE TABLE IF NOT EXISTS document_artifacts (
    id uuid PRIMARY KEY,
    job_id uuid NOT NULL REFERENCES document_jobs(id) ON DELETE CASCADE,
    storage_uri text NOT NULL,
    format text NOT NULL,
    size_in_bytes bigint NOT NULL,
    content_type text NOT NULL,
    created_at timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_document_artifacts_job_id ON document_artifacts(job_id);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_document_jobs_artifact_id'
    ) THEN
        ALTER TABLE document_jobs
            ADD CONSTRAINT fk_document_jobs_artifact_id
            FOREIGN KEY (artifact_id)
            REFERENCES document_artifacts(id);
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS document_templates (
    template_id text NOT NULL,
    version text NOT NULL,
    renderer text NOT NULL,
    registered_at timestamptz NOT NULL,
    CONSTRAINT pk_document_templates PRIMARY KEY (template_id, version)
);

CREATE TABLE IF NOT EXISTS idempotency_records (
    key text PRIMARY KEY,
    payload_hash text NOT NULL,
    job_id uuid NULL REFERENCES document_jobs(id),
    created_at timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_idempotency_records_job_id
    ON idempotency_records(job_id)
    WHERE job_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_document_jobs_claimable
    ON document_jobs(status, claim_expires_at, created_at);

INSERT INTO document_templates(template_id, version, renderer, registered_at)
VALUES ('sample', 'v1', 'placeholder', now())
ON CONFLICT (template_id, version) DO NOTHING;

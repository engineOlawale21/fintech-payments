CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911224613_InitialWalletPersistence') THEN
    CREATE TABLE wallets (
        id uuid NOT NULL,
        owner_id uuid NOT NULL,
        currency character(3) NOT NULL,
        available_balance numeric(19,4) NOT NULL,
        status character varying(16) NOT NULL,
        version bigint NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_wallets" PRIMARY KEY (id),
        CONSTRAINT ck_wallets_balance_non_negative CHECK (available_balance >= 0),
        CONSTRAINT ck_wallets_currency_length CHECK (char_length(currency) = 3)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911224613_InitialWalletPersistence') THEN
    CREATE UNIQUE INDEX ux_wallets_owner_currency ON wallets (owner_id, currency);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911224613_InitialWalletPersistence') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260911224613_InitialWalletPersistence', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911225524_AddUserAuthentication') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        email character varying(320) NOT NULL,
        normalized_email character varying(320) NOT NULL,
        password_hash character varying(512) NOT NULL,
        role character varying(16) NOT NULL,
        status character varying(16) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911225524_AddUserAuthentication') THEN
    CREATE UNIQUE INDEX ux_users_normalized_email ON users (normalized_email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911225524_AddUserAuthentication') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260911225524_AddUserAuthentication', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911230340_AddWalletOwnerForeignKey') THEN
    ALTER TABLE wallets ADD CONSTRAINT "FK_wallets_users_owner_id" FOREIGN KEY (owner_id) REFERENCES users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911230340_AddWalletOwnerForeignKey') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260911230340_AddWalletOwnerForeignKey', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911231104_AddDoubleEntryLedger') THEN
    CREATE TABLE ledger_transactions (
        id uuid NOT NULL,
        type character varying(24) NOT NULL,
        reference character varying(100) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ledger_transactions" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911231104_AddDoubleEntryLedger') THEN
    CREATE TABLE ledger_entries (
        id uuid NOT NULL,
        ledger_transaction_id uuid NOT NULL,
        wallet_id uuid NOT NULL,
        direction character varying(8) NOT NULL,
        amount numeric(19,4) NOT NULL,
        currency character(3) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ledger_entries" PRIMARY KEY (id),
        CONSTRAINT ck_ledger_entries_amount_positive CHECK (amount > 0),
        CONSTRAINT ck_ledger_entries_currency_length CHECK (char_length(currency) = 3),
        CONSTRAINT "FK_ledger_entries_ledger_transactions_ledger_transaction_id" FOREIGN KEY (ledger_transaction_id) REFERENCES ledger_transactions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_ledger_entries_wallets_wallet_id" FOREIGN KEY (wallet_id) REFERENCES wallets (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911231104_AddDoubleEntryLedger') THEN
    CREATE INDEX "IX_ledger_entries_ledger_transaction_id" ON ledger_entries (ledger_transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911231104_AddDoubleEntryLedger') THEN
    CREATE INDEX ix_ledger_entries_wallet_created_id ON ledger_entries (wallet_id, created_at, id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911231104_AddDoubleEntryLedger') THEN
    CREATE UNIQUE INDEX ux_ledger_transactions_reference ON ledger_transactions (reference);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911231104_AddDoubleEntryLedger') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260911231104_AddDoubleEntryLedger', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912025419_AddAtomicTransfers') THEN
    ALTER TABLE ledger_transactions ADD transfer_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912025419_AddAtomicTransfers') THEN
    CREATE TABLE transfers (
        id uuid NOT NULL,
        source_wallet_id uuid NOT NULL,
        destination_wallet_id uuid NOT NULL,
        amount numeric(19,4) NOT NULL,
        currency character(3) NOT NULL,
        reference character varying(100) NOT NULL,
        status character varying(16) NOT NULL,
        failure_reason character varying(200),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_transfers" PRIMARY KEY (id),
        CONSTRAINT ck_transfers_amount_positive CHECK (amount > 0),
        CONSTRAINT ck_transfers_distinct_wallets CHECK (source_wallet_id <> destination_wallet_id),
        CONSTRAINT "FK_transfers_wallets_destination_wallet_id" FOREIGN KEY (destination_wallet_id) REFERENCES wallets (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_transfers_wallets_source_wallet_id" FOREIGN KEY (source_wallet_id) REFERENCES wallets (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912025419_AddAtomicTransfers') THEN
    CREATE UNIQUE INDEX ux_ledger_transactions_transfer_id ON ledger_transactions (transfer_id) WHERE transfer_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912025419_AddAtomicTransfers') THEN
    CREATE INDEX ix_transfers_destination_created ON transfers (destination_wallet_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912025419_AddAtomicTransfers') THEN
    CREATE INDEX ix_transfers_source_created ON transfers (source_wallet_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912025419_AddAtomicTransfers') THEN
    CREATE UNIQUE INDEX ux_transfers_reference ON transfers (reference);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912025419_AddAtomicTransfers') THEN
    ALTER TABLE ledger_transactions ADD CONSTRAINT "FK_ledger_transactions_transfers_transfer_id" FOREIGN KEY (transfer_id) REFERENCES transfers (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912025419_AddAtomicTransfers') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260912025419_AddAtomicTransfers', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912030351_AddTransferIdempotency') THEN
    CREATE TABLE idempotency_records (
        id uuid NOT NULL,
        actor_id uuid NOT NULL,
        operation character varying(100) NOT NULL,
        key character varying(100) NOT NULL,
        request_hash character(64) NOT NULL,
        state character varying(24) NOT NULL,
        transfer_id uuid,
        response_status_code integer,
        response_body jsonb,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_idempotency_records" PRIMARY KEY (id),
        CONSTRAINT "FK_idempotency_records_transfers_transfer_id" FOREIGN KEY (transfer_id) REFERENCES transfers (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_idempotency_records_users_actor_id" FOREIGN KEY (actor_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912030351_AddTransferIdempotency') THEN
    CREATE INDEX ix_idempotency_expires_at ON idempotency_records (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912030351_AddTransferIdempotency') THEN
    CREATE UNIQUE INDEX ux_idempotency_actor_operation_key ON idempotency_records (actor_id, operation, key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912030351_AddTransferIdempotency') THEN
    CREATE UNIQUE INDEX ux_idempotency_transfer_id ON idempotency_records (transfer_id) WHERE transfer_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912030351_AddTransferIdempotency') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260912030351_AddTransferIdempotency', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912090502_AddPaymentWebhooks') THEN
    CREATE TABLE webhook_events (
        "Id" uuid NOT NULL,
        "Provider" character varying(64) NOT NULL,
        "ExternalEventId" character varying(128) NOT NULL,
        "EventType" character varying(128) NOT NULL,
        "ResourceReference" character varying(128) NOT NULL,
        "PayloadHash" char(64) NOT NULL,
        "Payload" jsonb NOT NULL,
        "ProviderTimestamp" timestamp with time zone NOT NULL,
        "ReceivedAt" timestamp with time zone NOT NULL,
        "ProcessedAt" timestamp with time zone,
        "Status" character varying(32) NOT NULL,
        "FailureReason" character varying(512),
        CONSTRAINT "PK_webhook_events" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912090502_AddPaymentWebhooks') THEN
    CREATE UNIQUE INDEX "IX_webhook_events_Provider_ExternalEventId" ON webhook_events ("Provider", "ExternalEventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912090502_AddPaymentWebhooks') THEN
    CREATE INDEX "IX_webhook_events_Status_ReceivedAt" ON webhook_events ("Status", "ReceivedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912090502_AddPaymentWebhooks') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260912090502_AddPaymentWebhooks', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092024_AddReconciliation') THEN
    CREATE TABLE reconciliation_runs (
        id uuid NOT NULL,
        provider character varying(64) NOT NULL,
        period_start timestamp with time zone NOT NULL,
        period_end timestamp with time zone NOT NULL,
        input_checksum char(64) NOT NULL,
        matched_count integer NOT NULL,
        discrepancy_count integer NOT NULL,
        internal_total numeric(19,4) NOT NULL,
        external_total numeric(19,4) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_reconciliation_runs" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092024_AddReconciliation') THEN
    CREATE TABLE reconciliation_items (
        id uuid NOT NULL,
        reconciliation_run_id uuid NOT NULL,
        reference character varying(100) NOT NULL,
        category character varying(32) NOT NULL,
        internal_amount numeric(19,4),
        external_amount numeric(19,4),
        currency character(3) NOT NULL,
        resolution_note character varying(500),
        resolved_by uuid,
        resolved_at timestamp with time zone,
        CONSTRAINT "PK_reconciliation_items" PRIMARY KEY (id),
        CONSTRAINT "FK_reconciliation_items_reconciliation_runs_reconciliation_run~" FOREIGN KEY (reconciliation_run_id) REFERENCES reconciliation_runs (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092024_AddReconciliation') THEN
    CREATE INDEX ix_reconciliation_items_run_category ON reconciliation_items (reconciliation_run_id, category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092024_AddReconciliation') THEN
    CREATE UNIQUE INDEX ux_reconciliation_runs_identity ON reconciliation_runs (provider, period_start, period_end, input_checksum);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092024_AddReconciliation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260912092024_AddReconciliation', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092948_AddSettlements') THEN
    CREATE TABLE settlement_batches (
        id uuid NOT NULL,
        reconciliation_run_id uuid NOT NULL,
        period_start timestamp with time zone NOT NULL,
        period_end timestamp with time zone NOT NULL,
        currency character(3) NOT NULL,
        gross_amount numeric(19,4) NOT NULL,
        fee_amount numeric(19,4) NOT NULL,
        adjustment_amount numeric(19,4) NOT NULL,
        adjustment_reason character varying(500),
        net_amount numeric(19,4) NOT NULL,
        status character varying(16) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        finalized_at timestamp with time zone,
        finalized_by uuid,
        CONSTRAINT "PK_settlement_batches" PRIMARY KEY (id),
        CONSTRAINT "FK_settlement_batches_reconciliation_runs_reconciliation_run_id" FOREIGN KEY (reconciliation_run_id) REFERENCES reconciliation_runs (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092948_AddSettlements') THEN
    CREATE TABLE settlement_items (
        id uuid NOT NULL,
        settlement_batch_id uuid NOT NULL,
        reconciliation_item_id uuid NOT NULL,
        CONSTRAINT "PK_settlement_items" PRIMARY KEY (id),
        CONSTRAINT "FK_settlement_items_settlement_batches_settlement_batch_id" FOREIGN KEY (settlement_batch_id) REFERENCES settlement_batches (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092948_AddSettlements') THEN
    CREATE INDEX ix_settlement_batches_period ON settlement_batches (status, currency, period_start, period_end);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092948_AddSettlements') THEN
    CREATE INDEX "IX_settlement_batches_reconciliation_run_id" ON settlement_batches (reconciliation_run_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092948_AddSettlements') THEN
    CREATE INDEX "IX_settlement_items_settlement_batch_id" ON settlement_items (settlement_batch_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092948_AddSettlements') THEN
    CREATE UNIQUE INDEX ux_settlement_items_reconciliation_item ON settlement_items (reconciliation_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912092948_AddSettlements') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260912092948_AddSettlements', '10.0.4');
    END IF;
END $EF$;
COMMIT;


using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FintechPayments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_actor_created",
                table: "audit_events",
                columns: new[] { "actor_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_correlation",
                table: "audit_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_entity_created",
                table: "audit_events",
                columns: new[] { "entity_type", "entity_id", "created_at" });

            migrationBuilder.Sql("""
                CREATE FUNCTION prevent_audit_event_mutation()
                RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'audit_events is append-only';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER audit_events_append_only
                BEFORE UPDATE OR DELETE ON audit_events
                FOR EACH ROW EXECUTE FUNCTION prevent_audit_event_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS prevent_audit_event_mutation();");
        }
    }
}

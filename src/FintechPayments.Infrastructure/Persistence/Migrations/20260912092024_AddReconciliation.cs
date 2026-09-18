using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FintechPayments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reconciliation_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    input_checksum = table.Column<string>(type: "char(64)", nullable: false),
                    matched_count = table.Column<int>(type: "integer", nullable: false),
                    discrepancy_count = table.Column<int>(type: "integer", nullable: false),
                    internal_total = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    external_total = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reconciliation_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reconciliation_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reconciliation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    internal_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    external_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    resolution_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reconciliation_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_reconciliation_items_reconciliation_runs_reconciliation_run~",
                        column: x => x.reconciliation_run_id,
                        principalTable: "reconciliation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reconciliation_items_run_category",
                table: "reconciliation_items",
                columns: new[] { "reconciliation_run_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ux_reconciliation_runs_identity",
                table: "reconciliation_runs",
                columns: new[] { "provider", "period_start", "period_end", "input_checksum" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reconciliation_items");

            migrationBuilder.DropTable(
                name: "reconciliation_runs");
        }
    }
}

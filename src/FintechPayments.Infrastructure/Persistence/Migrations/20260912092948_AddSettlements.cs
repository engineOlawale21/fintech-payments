using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FintechPayments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "settlement_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reconciliation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    fee_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    adjustment_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    adjustment_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    net_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finalized_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settlement_batches", x => x.id);
                    table.ForeignKey(
                        name: "FK_settlement_batches_reconciliation_runs_reconciliation_run_id",
                        column: x => x.reconciliation_run_id,
                        principalTable: "reconciliation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "settlement_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    settlement_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reconciliation_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settlement_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_settlement_items_settlement_batches_settlement_batch_id",
                        column: x => x.settlement_batch_id,
                        principalTable: "settlement_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_settlement_batches_period",
                table: "settlement_batches",
                columns: new[] { "status", "currency", "period_start", "period_end" });

            migrationBuilder.CreateIndex(
                name: "IX_settlement_batches_reconciliation_run_id",
                table: "settlement_batches",
                column: "reconciliation_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_items_settlement_batch_id",
                table: "settlement_items",
                column: "settlement_batch_id");

            migrationBuilder.CreateIndex(
                name: "ux_settlement_items_reconciliation_item",
                table: "settlement_items",
                column: "reconciliation_item_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "settlement_items");

            migrationBuilder.DropTable(
                name: "settlement_batches");
        }
    }
}

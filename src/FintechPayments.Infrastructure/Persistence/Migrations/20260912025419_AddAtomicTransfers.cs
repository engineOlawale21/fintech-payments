using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FintechPayments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAtomicTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "transfer_id",
                table: "ledger_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => x.id);
                    table.CheckConstraint("ck_transfers_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_transfers_distinct_wallets", "source_wallet_id <> destination_wallet_id");
                    table.ForeignKey(
                        name: "FK_transfers_wallets_destination_wallet_id",
                        column: x => x.destination_wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfers_wallets_source_wallet_id",
                        column: x => x.source_wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_ledger_transactions_transfer_id",
                table: "ledger_transactions",
                column: "transfer_id",
                unique: true,
                filter: "transfer_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_transfers_destination_created",
                table: "transfers",
                columns: new[] { "destination_wallet_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_transfers_source_created",
                table: "transfers",
                columns: new[] { "source_wallet_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_transfers_reference",
                table: "transfers",
                column: "reference",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ledger_transactions_transfers_transfer_id",
                table: "ledger_transactions",
                column: "transfer_id",
                principalTable: "transfers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ledger_transactions_transfers_transfer_id",
                table: "ledger_transactions");

            migrationBuilder.DropTable(
                name: "transfers");

            migrationBuilder.DropIndex(
                name: "ux_ledger_transactions_transfer_id",
                table: "ledger_transactions");

            migrationBuilder.DropColumn(
                name: "transfer_id",
                table: "ledger_transactions");
        }
    }
}

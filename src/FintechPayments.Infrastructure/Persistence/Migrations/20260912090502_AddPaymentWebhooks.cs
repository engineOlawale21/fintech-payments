using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FintechPayments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentWebhooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webhook_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalEventId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResourceReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PayloadHash = table.Column<string>(type: "char(64)", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    ProviderTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_events_Provider_ExternalEventId",
                table: "webhook_events",
                columns: new[] { "Provider", "ExternalEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_webhook_events_Status_ReceivedAt",
                table: "webhook_events",
                columns: new[] { "Status", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_events");
        }
    }
}

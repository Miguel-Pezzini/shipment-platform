using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutboxRetryAndInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_events_ProcessedAtUtc",
                table: "outbox_events");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "outbox_events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "outbox_events",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAtUtc",
                table: "outbox_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PoisonedAtUtc",
                table: "outbox_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.MessageId, x.ConsumerName });
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_ProcessedAtUtc_PoisonedAtUtc_NextAttemptAtUtc",
                table: "outbox_events",
                columns: new[] { "ProcessedAtUtc", "PoisonedAtUtc", "NextAttemptAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_events_ProcessedAtUtc_PoisonedAtUtc_NextAttemptAtUtc",
                table: "outbox_events");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "outbox_events");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "outbox_events");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                table: "outbox_events");

            migrationBuilder.DropColumn(
                name: "PoisonedAtUtc",
                table: "outbox_events");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_ProcessedAtUtc",
                table: "outbox_events",
                column: "ProcessedAtUtc");
        }
    }
}

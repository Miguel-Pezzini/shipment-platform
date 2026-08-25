using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MassTransit8ModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxState_BusName_Created",
                table: "OutboxState");

            migrationBuilder.DropColumn(
                name: "BusName",
                table: "OutboxState");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                table: "OutboxMessage",
                column: "ExpirationTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxState_Created",
                table: "OutboxState");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                table: "OutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                table: "OutboxMessage");

            migrationBuilder.AddColumn<string>(
                name: "BusName",
                table: "OutboxState",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_BusName_Created",
                table: "OutboxState",
                columns: new[] { "BusName", "Created" });
        }
    }
}

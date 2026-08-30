using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shipment_timeline",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackingCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_timeline", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shipment_timeline_MessageId",
                table: "shipment_timeline",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipment_timeline_ShipmentId",
                table: "shipment_timeline",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_timeline_TrackingCode",
                table: "shipment_timeline",
                column: "TrackingCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipment_timeline");
        }
    }
}

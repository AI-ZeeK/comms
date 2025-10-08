using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace comms.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification",
                schema: "notifications",
                columns: table => new
                {
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notification_type = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    link_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification", x => x.notification_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_notification_type",
                schema: "notifications",
                table: "notification",
                column: "notification_type");

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipient_user_id",
                schema: "notifications",
                table: "notification",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_sender_user_id",
                schema: "notifications",
                table: "notification",
                column: "sender_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification",
                schema: "notifications");
        }
    }
}

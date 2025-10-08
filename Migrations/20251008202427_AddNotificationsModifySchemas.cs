using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace comms.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsModifySchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.RenameTable(
                name: "vapid_keys",
                schema: "communications",
                newName: "vapid_keys",
                newSchema: "notifications");

            migrationBuilder.RenameTable(
                name: "push_subscriptions",
                schema: "communications",
                newName: "push_subscriptions",
                newSchema: "notifications");

            migrationBuilder.RenameTable(
                name: "meta_data",
                schema: "notifications",
                newName: "meta_data",
                newSchema: "admin");

            migrationBuilder.AddColumn<int>(
                name: "user_type",
                schema: "notifications",
                table: "push_subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_type",
                schema: "notifications",
                table: "push_subscriptions");

            migrationBuilder.RenameTable(
                name: "vapid_keys",
                schema: "notifications",
                newName: "vapid_keys",
                newSchema: "communications");

            migrationBuilder.RenameTable(
                name: "push_subscriptions",
                schema: "notifications",
                newName: "push_subscriptions",
                newSchema: "communications");

            migrationBuilder.RenameTable(
                name: "meta_data",
                schema: "admin",
                newName: "meta_data",
                newSchema: "notifications");
        }
    }
}

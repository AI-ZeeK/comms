using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace comms.Migrations
{
    /// <inheritdoc />
    public partial class AdminMetaData1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.RenameTable(
                name: "meta_data",
                schema: "communications",
                newName: "meta_data",
                newSchema: "notifications");

            migrationBuilder.AddColumn<bool>(
                name: "enable_failed_payments",
                schema: "notifications",
                table: "meta_data",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "enable_new_user",
                schema: "notifications",
                table: "meta_data",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "enable_newsletter",
                schema: "notifications",
                table: "meta_data",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "enable_password_reset",
                schema: "notifications",
                table: "meta_data",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "enable_security_alerts",
                schema: "notifications",
                table: "meta_data",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "enable_system_error",
                schema: "notifications",
                table: "meta_data",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "enable_welcome_email",
                schema: "notifications",
                table: "meta_data",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "enable_failed_payments",
                schema: "notifications",
                table: "meta_data");

            migrationBuilder.DropColumn(
                name: "enable_new_user",
                schema: "notifications",
                table: "meta_data");

            migrationBuilder.DropColumn(
                name: "enable_newsletter",
                schema: "notifications",
                table: "meta_data");

            migrationBuilder.DropColumn(
                name: "enable_password_reset",
                schema: "notifications",
                table: "meta_data");

            migrationBuilder.DropColumn(
                name: "enable_security_alerts",
                schema: "notifications",
                table: "meta_data");

            migrationBuilder.DropColumn(
                name: "enable_system_error",
                schema: "notifications",
                table: "meta_data");

            migrationBuilder.DropColumn(
                name: "enable_welcome_email",
                schema: "notifications",
                table: "meta_data");

            migrationBuilder.RenameTable(
                name: "meta_data",
                schema: "notifications",
                newName: "meta_data",
                newSchema: "communications");
        }
    }
}

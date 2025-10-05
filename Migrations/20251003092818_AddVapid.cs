using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace comms.Migrations
{
    /// <inheritdoc />
    public partial class AddVapid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "communications");

            migrationBuilder.CreateTable(
                name: "chats",
                schema: "communications",
                columns: table => new
                {
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    chat_type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chats", x => x.chat_id);
                });

            migrationBuilder.CreateTable(
                name: "push_subscriptions",
                schema: "communications",
                columns: table => new
                {
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    p256dh = table.Column<string>(type: "text", nullable: true),
                    auth = table.Column<string>(type: "text", nullable: true),
                    platform = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_subscriptions", x => x.subscription_id);
                });

            migrationBuilder.CreateTable(
                name: "vapid_keys",
                schema: "communications",
                columns: table => new
                {
                    vapid_key_id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vapid_keys", x => x.vapid_key_id);
                });

            migrationBuilder.CreateTable(
                name: "chat_participants",
                schema: "communications",
                columns: table => new
                {
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unread_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_participants", x => new { x.chat_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_chat_participants_chats_chat_id",
                        column: x => x.chat_id,
                        principalSchema: "communications",
                        principalTable: "chats",
                        principalColumn: "chat_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "communications",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    media_urls = table.Column<string[]>(type: "text[]", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration = table.Column<int>(type: "integer", nullable: true),
                    file_url = table.Column<string>(type: "text", nullable: true),
                    file_size = table.Column<int>(type: "integer", nullable: true),
                    file_type = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.message_id);
                    table.ForeignKey(
                        name: "FK_messages_chats_chat_id",
                        column: x => x.chat_id,
                        principalSchema: "communications",
                        principalTable: "chats",
                        principalColumn: "chat_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "unread_message_counts",
                schema: "communications",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    last_read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unread_message_counts", x => new { x.user_id, x.chat_id });
                    table.ForeignKey(
                        name: "FK_unread_message_counts_chats_chat_id",
                        column: x => x.chat_id,
                        principalSchema: "communications",
                        principalTable: "chats",
                        principalColumn: "chat_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_reads",
                schema: "communications",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_reads", x => new { x.message_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_message_reads_messages_message_id",
                        column: x => x.message_id,
                        principalSchema: "communications",
                        principalTable: "messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_participants_user_id",
                schema: "communications",
                table: "chat_participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_message_reads_user_id",
                schema: "communications",
                table: "message_reads",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_chat_id",
                schema: "communications",
                table: "messages",
                column: "chat_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_sender_id",
                schema: "communications",
                table: "messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_push_subscriptions_user_id",
                schema: "communications",
                table: "push_subscriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_push_subscriptions_user_id_endpoint",
                schema: "communications",
                table: "push_subscriptions",
                columns: new[] { "user_id", "endpoint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unread_message_counts_chat_id",
                schema: "communications",
                table: "unread_message_counts",
                column: "chat_id");

            migrationBuilder.CreateIndex(
                name: "IX_unread_message_counts_user_id",
                schema: "communications",
                table: "unread_message_counts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_vapid_keys_public_key",
                schema: "communications",
                table: "vapid_keys",
                column: "public_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_participants",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "message_reads",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "push_subscriptions",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "unread_message_counts",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "vapid_keys",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "chats",
                schema: "communications");
        }
    }
}

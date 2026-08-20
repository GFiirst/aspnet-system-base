using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspnet_system_base.Features.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EncryptIpInRefreshTokensAndAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_email",
                table: "user");

            migrationBuilder.DropColumn(
                name: "email",
                table: "user");

            migrationBuilder.DropColumn(
                name: "ip",
                table: "refresh_token");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "user_email",
                table: "audit_logs");

            migrationBuilder.AddColumn<string>(
                name: "email_encrypted",
                table: "user",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email_hash",
                table: "user",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ip_encrypted",
                table: "refresh_token",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ip_hash",
                table: "refresh_token",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "entity_name",
                table: "audit_logs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ip_address_encrypted",
                table: "audit_logs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ip_address_hash",
                table: "audit_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_email_hash",
                table: "user",
                column: "email_hash",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_token_ip_hash",
                table: "refresh_token",
                column: "ip_hash");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_ip_address_hash",
                table: "audit_logs",
                column: "ip_address_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_email_hash",
                table: "user");

            migrationBuilder.DropIndex(
                name: "IX_refresh_token_ip_hash",
                table: "refresh_token");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_ip_address_hash",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "email_encrypted",
                table: "user");

            migrationBuilder.DropColumn(
                name: "email_hash",
                table: "user");

            migrationBuilder.DropColumn(
                name: "ip_encrypted",
                table: "refresh_token");

            migrationBuilder.DropColumn(
                name: "ip_hash",
                table: "refresh_token");

            migrationBuilder.DropColumn(
                name: "ip_address_encrypted",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "ip_address_hash",
                table: "audit_logs");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "user",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ip",
                table: "refresh_token",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "entity_name",
                table: "audit_logs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_email",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_email",
                table: "user",
                column: "email",
                unique: true,
                filter: "deleted_at IS NULL");
        }
    }
}

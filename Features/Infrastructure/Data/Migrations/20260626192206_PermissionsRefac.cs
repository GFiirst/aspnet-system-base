using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspnet_autenticacao.Migrations
{
    /// <inheritdoc />
    public partial class PermissionsRefac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_role_permission_Permissions_permission_id",
                table: "role_permission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permissions",
                table: "Permissions");

            migrationBuilder.RenameTable(
                name: "Permissions",
                newName: "permissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_permissions",
                table: "permissions",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_role_permission_permissions_permission_id",
                table: "role_permission",
                column: "permission_id",
                principalTable: "permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_role_permission_permissions_permission_id",
                table: "role_permission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_permissions",
                table: "permissions");

            migrationBuilder.RenameTable(
                name: "permissions",
                newName: "Permissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Permissions",
                table: "Permissions",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_role_permission_Permissions_permission_id",
                table: "role_permission",
                column: "permission_id",
                principalTable: "Permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

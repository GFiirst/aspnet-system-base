using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspnet_system_base.Features.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccentInsensitiveSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
            migrationBuilder.Sql(
                """
                CREATE FUNCTION unaccented_ilike(
                    match_expression text,
                    pattern text,
                    escape_character text)
                RETURNS boolean
                LANGUAGE sql
                STABLE
                PARALLEL SAFE
                AS $function$
                    SELECT unaccent(match_expression)
                        ILIKE unaccent(pattern)
                        ESCAPE escape_character;
                $function$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP FUNCTION IF EXISTS unaccented_ilike(text, text, text);");
        }
    }
}

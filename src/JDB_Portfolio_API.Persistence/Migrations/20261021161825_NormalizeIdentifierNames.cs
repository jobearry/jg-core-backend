using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDB_Portfolio_API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeIdentifierNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Identity sequence names are assigned by convention when the table is
            // created and are not part of the EF model, so EF cannot generate this
            // rename. The old chain left it stale after the recommendations ->
            // recommendation table rename.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('public.recommendations_id_seq') IS NOT NULL
                       AND to_regclass('public.recommendation_id_seq') IS NULL THEN
                        ALTER SEQUENCE public.recommendations_id_seq RENAME TO recommendation_id_seq;
                    END IF;
                END $$;
                """);

            migrationBuilder.DropPrimaryKey(
                name: "recommendations_pkey",
                table: "recommendation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_project_tech_stack",
                table: "project_tech_stack");

            migrationBuilder.AddPrimaryKey(
                name: "recommendation_pkey",
                table: "recommendation",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "project_tech_stack_pkey",
                table: "project_tech_stack",
                columns: new[] { "project_id", "tech_stack_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('public.recommendation_id_seq') IS NOT NULL
                       AND to_regclass('public.recommendations_id_seq') IS NULL THEN
                        ALTER SEQUENCE public.recommendation_id_seq RENAME TO recommendations_id_seq;
                    END IF;
                END $$;
                """);

            migrationBuilder.DropPrimaryKey(
                name: "recommendation_pkey",
                table: "recommendation");

            migrationBuilder.DropPrimaryKey(
                name: "project_tech_stack_pkey",
                table: "project_tech_stack");

            migrationBuilder.AddPrimaryKey(
                name: "recommendations_pkey",
                table: "recommendation",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_project_tech_stack",
                table: "project_tech_stack",
                columns: new[] { "project_id", "tech_stack_id" });
        }
    }
}

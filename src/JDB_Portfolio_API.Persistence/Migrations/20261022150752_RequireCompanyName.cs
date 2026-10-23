using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDB_Portfolio_API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequireCompanyName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF scaffolds `defaultValue: ""` here, which would silently invent an empty
            // company for any NULL row — and then the CHECK below would reject it anyway.
            // Removed on purpose (this migration has never been applied): if a NULL row
            // exists, `SET NOT NULL` must fail loudly instead of fabricating data.
            migrationBuilder.AlterColumn<string>(
                name: "company_name",
                table: "experience",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            // NOT NULL alone still allows '' — the loophole `role` has. btrim also
            // rejects all-whitespace, so the schema means what Company.Name means.
            migrationBuilder.AddCheckConstraint(
                name: "ck_experience_company_name_not_blank",
                table: "experience",
                sql: "btrim(company_name) <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_experience_company_name_not_blank",
                table: "experience");

            migrationBuilder.AlterColumn<string>(
                name: "company_name",
                table: "experience",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProdBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tech_stack_tech_stack_category_tech_stack_category_id",
                table: "tech_stack");

            migrationBuilder.AddForeignKey(
                name: "FK_tech_stack_tech_stack_category_id",
                table: "tech_stack",
                column: "tech_stack_category_id",
                principalTable: "tech_stack_category",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tech_stack_tech_stack_category_id",
                table: "tech_stack");

            migrationBuilder.AddForeignKey(
                name: "FK_tech_stack_tech_stack_category_tech_stack_category_id",
                table: "tech_stack",
                column: "tech_stack_category_id",
                principalTable: "tech_stack_category",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

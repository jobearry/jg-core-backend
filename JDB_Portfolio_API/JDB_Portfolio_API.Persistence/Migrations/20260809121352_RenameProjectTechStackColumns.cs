using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDB_Portfolio_API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameProjectTechStackColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_experience_experience_id",
                table: "project");

            migrationBuilder.DropForeignKey(
                name: "FK_project_tech_stack_project_ProjectId",
                table: "project_tech_stack");

            migrationBuilder.DropForeignKey(
                name: "FK_project_tech_stack_tech_stack_TechStackId",
                table: "project_tech_stack");

            migrationBuilder.RenameColumn(
                name: "TechStackId",
                table: "project_tech_stack",
                newName: "tech_stack_id");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "project_tech_stack",
                newName: "project_id");

            migrationBuilder.RenameIndex(
                name: "IX_project_tech_stack_TechStackId",
                table: "project_tech_stack",
                newName: "IX_project_tech_stack_tech_stack_id");

            migrationBuilder.AddForeignKey(
                name: "FK_project_experience_experience_id",
                table: "project",
                column: "experience_id",
                principalTable: "experience",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_project_tech_stack_project_project_id",
                table: "project_tech_stack",
                column: "project_id",
                principalTable: "project",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_project_tech_stack_tech_stack_tech_stack_id",
                table: "project_tech_stack",
                column: "tech_stack_id",
                principalTable: "tech_stack",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_experience_experience_id",
                table: "project");

            migrationBuilder.DropForeignKey(
                name: "FK_project_tech_stack_project_project_id",
                table: "project_tech_stack");

            migrationBuilder.DropForeignKey(
                name: "FK_project_tech_stack_tech_stack_tech_stack_id",
                table: "project_tech_stack");

            migrationBuilder.RenameColumn(
                name: "tech_stack_id",
                table: "project_tech_stack",
                newName: "TechStackId");

            migrationBuilder.RenameColumn(
                name: "project_id",
                table: "project_tech_stack",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_project_tech_stack_tech_stack_id",
                table: "project_tech_stack",
                newName: "IX_project_tech_stack_TechStackId");

            migrationBuilder.AddForeignKey(
                name: "FK_project_experience_experience_id",
                table: "project",
                column: "experience_id",
                principalTable: "experience",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_project_tech_stack_project_ProjectId",
                table: "project_tech_stack",
                column: "ProjectId",
                principalTable: "project",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_project_tech_stack_tech_stack_TechStackId",
                table: "project_tech_stack",
                column: "TechStackId",
                principalTable: "tech_stack",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

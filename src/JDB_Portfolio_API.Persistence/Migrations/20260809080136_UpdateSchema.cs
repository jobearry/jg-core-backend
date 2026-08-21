using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDB_Portfolio_API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTechStack_project_ProjectId",
                table: "ProjectTechStack");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTechStack_tech_stack_TechStackId",
                table: "ProjectTechStack");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectTechStack",
                table: "ProjectTechStack");

            migrationBuilder.RenameTable(
                name: "ProjectTechStack",
                newName: "project_tech_stack");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectTechStack_TechStackId",
                table: "project_tech_stack",
                newName: "IX_project_tech_stack_TechStackId");

            migrationBuilder.AddColumn<int>(
                name: "ExperienceAtId",
                table: "project",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_project_tech_stack",
                table: "project_tech_stack",
                columns: new[] { "ProjectId", "TechStackId" });

            migrationBuilder.CreateIndex(
                name: "IX_tech_stack_tech_stack_category_id",
                table: "tech_stack",
                column: "tech_stack_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_ExperienceAtId",
                table: "project",
                column: "ExperienceAtId");

            migrationBuilder.AddForeignKey(
                name: "FK_project_experience_ExperienceAtId",
                table: "project",
                column: "ExperienceAtId",
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

            migrationBuilder.AddForeignKey(
                name: "FK_tech_stack_tech_stack_category_tech_stack_category_id",
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
                name: "FK_project_experience_ExperienceAtId",
                table: "project");

            migrationBuilder.DropForeignKey(
                name: "FK_project_tech_stack_project_ProjectId",
                table: "project_tech_stack");

            migrationBuilder.DropForeignKey(
                name: "FK_project_tech_stack_tech_stack_TechStackId",
                table: "project_tech_stack");

            migrationBuilder.DropForeignKey(
                name: "FK_tech_stack_tech_stack_category_tech_stack_category_id",
                table: "tech_stack");

            migrationBuilder.DropIndex(
                name: "IX_tech_stack_tech_stack_category_id",
                table: "tech_stack");

            migrationBuilder.DropIndex(
                name: "IX_project_ExperienceAtId",
                table: "project");

            migrationBuilder.DropPrimaryKey(
                name: "PK_project_tech_stack",
                table: "project_tech_stack");

            migrationBuilder.DropColumn(
                name: "ExperienceAtId",
                table: "project");

            migrationBuilder.RenameTable(
                name: "project_tech_stack",
                newName: "ProjectTechStack");

            migrationBuilder.RenameIndex(
                name: "IX_project_tech_stack_TechStackId",
                table: "ProjectTechStack",
                newName: "IX_ProjectTechStack_TechStackId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectTechStack",
                table: "ProjectTechStack",
                columns: new[] { "ProjectId", "TechStackId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTechStack_project_ProjectId",
                table: "ProjectTechStack",
                column: "ProjectId",
                principalTable: "project",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTechStack_tech_stack_TechStackId",
                table: "ProjectTechStack",
                column: "TechStackId",
                principalTable: "tech_stack",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

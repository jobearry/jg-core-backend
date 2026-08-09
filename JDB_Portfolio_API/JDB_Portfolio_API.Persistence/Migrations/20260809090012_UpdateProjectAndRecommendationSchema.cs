using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDB_Portfolio_API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProjectAndRecommendationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_experience_ExperienceAtId",
                table: "project");

            migrationBuilder.DropIndex(
                name: "IX_project_ExperienceAtId",
                table: "project");

            migrationBuilder.DropColumn(
                name: "ExperienceAtId",
                table: "project");

            migrationBuilder.RenameTable(
                name: "recommendations",
                newName: "recommendation");

            migrationBuilder.CreateIndex(
                name: "IX_project_experience_id",
                table: "project",
                column: "experience_id");

            migrationBuilder.AddForeignKey(
                name: "FK_project_experience_experience_id",
                table: "project",
                column: "experience_id",
                principalTable: "experience",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_experience_experience_id",
                table: "project");

            migrationBuilder.DropIndex(
                name: "IX_project_experience_id",
                table: "project");

            migrationBuilder.RenameTable(
                name: "recommendation",
                newName: "recommendations");

            migrationBuilder.AddColumn<int>(
                name: "ExperienceAtId",
                table: "project",
                type: "integer",
                nullable: true);

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
        }
    }
}

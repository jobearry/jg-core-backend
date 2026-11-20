using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDB_Portfolio_API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExperienceFinishedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "finished_at",
                table: "experience",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "finished_at",
                table: "experience");
        }
    }
}

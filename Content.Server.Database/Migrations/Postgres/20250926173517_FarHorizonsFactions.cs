using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class FarHorizonsFactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_profile_id_job_name",
                table: "job");

            migrationBuilder.AddColumn<string>(
                name: "faction_name",
                table: "job_priority_entry",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "faction_name",
                table: "job",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_job_profile_id_job_name_faction_name",
                table: "job",
                columns: new[] { "profile_id", "job_name", "faction_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_profile_id_job_name_faction_name",
                table: "job");

            migrationBuilder.DropColumn(
                name: "faction_name",
                table: "job_priority_entry");

            migrationBuilder.DropColumn(
                name: "faction_name",
                table: "job");

            migrationBuilder.CreateIndex(
                name: "IX_job_profile_id_job_name",
                table: "job",
                columns: new[] { "profile_id", "job_name" },
                unique: true);
        }
    }
}

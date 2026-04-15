using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Symspeech : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "silicon_voice",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "voice",
                table: "profile");

            migrationBuilder.CreateTable(
                name: "fh_symspeech",
                columns: table => new
                {
                    fh_symspeech_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    voice = table.Column<string>(type: "TEXT", nullable: false),
                    pitch = table.Column<int>(type: "INTEGER", nullable: false),
                    speed = table.Column<float>(type: "REAL", nullable: false),
                    pause = table.Column<float>(type: "REAL", nullable: false),
                    polyphony = table.Column<int>(type: "INTEGER", nullable: false),
                    volume = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fh_symspeech", x => x.fh_symspeech_id);
                });

            migrationBuilder.CreateTable(
                name: "far_horizons_profile",
                columns: table => new
                {
                    far_horizons_profile_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    symspeech_id = table.Column<int>(type: "INTEGER", nullable: true),
                    silicon_symspeech_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_far_horizons_profile", x => x.far_horizons_profile_id);
                    table.ForeignKey(
                        name: "FK_far_horizons_profile_fh_symspeech_silicon_symspeech_id",
                        column: x => x.silicon_symspeech_id,
                        principalTable: "fh_symspeech",
                        principalColumn: "fh_symspeech_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_far_horizons_profile_fh_symspeech_symspeech_id",
                        column: x => x.symspeech_id,
                        principalTable: "fh_symspeech",
                        principalColumn: "fh_symspeech_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_far_horizons_profile_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_far_horizons_profile_profile_id",
                table: "far_horizons_profile",
                column: "profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_far_horizons_profile_silicon_symspeech_id",
                table: "far_horizons_profile",
                column: "silicon_symspeech_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_far_horizons_profile_symspeech_id",
                table: "far_horizons_profile",
                column: "symspeech_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "far_horizons_profile");

            migrationBuilder.DropTable(
                name: "fh_symspeech");

            migrationBuilder.AddColumn<string>(
                name: "silicon_voice",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "voice",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}

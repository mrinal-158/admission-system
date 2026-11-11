using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAdmission.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropClassNameUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_classes_normalized_name",
                table: "classes");

            migrationBuilder.DropColumn(
                name: "normalized_name",
                table: "classes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_name",
                table: "classes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Backfill before restoring the unique index. Rolling back still fails if classes
            // with the same name were created after this migration: that data cannot satisfy
            // the old rule.
            migrationBuilder.Sql("UPDATE classes SET normalized_name = UPPER(TRIM(name));");

            migrationBuilder.CreateIndex(
                name: "ux_classes_normalized_name",
                table: "classes",
                column: "normalized_name",
                unique: true);
        }
    }
}

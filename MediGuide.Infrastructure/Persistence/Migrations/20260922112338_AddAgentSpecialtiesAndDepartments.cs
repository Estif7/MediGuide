using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediGuide.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentSpecialtiesAndDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Agents",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specialty",
                table: "Agents",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Agents",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Department",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "Specialty",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Agents");
        }
    }
}

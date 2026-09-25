using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediGuide.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingReferralApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReferralPendingApproval",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReferralClinicalNotes",
                table: "Bookings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralReason",
                table: "Bookings",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferredToAgentId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ReferredToAgentId",
                table: "Bookings",
                column: "ReferredToAgentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Agents_ReferredToAgentId",
                table: "Bookings",
                column: "ReferredToAgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Agents_ReferredToAgentId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ReferredToAgentId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsReferralPendingApproval",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReferralClinicalNotes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReferralReason",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReferredToAgentId",
                table: "Bookings");
        }
    }
}

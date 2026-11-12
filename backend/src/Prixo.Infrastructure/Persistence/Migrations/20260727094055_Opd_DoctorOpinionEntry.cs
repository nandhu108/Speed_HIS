using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Opd_DoctorOpinionEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "opinion_entered_at",
                schema: "opd",
                table: "referrals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "opinion_notes",
                schema: "opd",
                table: "referrals",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "opinion_entered_at",
                schema: "opd",
                table: "referrals");

            migrationBuilder.DropColumn(
                name: "opinion_notes",
                schema: "opd",
                table: "referrals");
        }
    }
}

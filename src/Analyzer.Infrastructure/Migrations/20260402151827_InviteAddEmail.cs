using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InviteAddEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "target_email",
                table: "invites",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "target_email",
                table: "invites");
        }
    }
}

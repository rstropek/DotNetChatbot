using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatBotDb.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionData",
                table: "Conversations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionData",
                table: "Conversations");
        }
    }
}

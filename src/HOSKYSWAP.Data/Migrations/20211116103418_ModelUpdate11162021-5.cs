using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HOSKYSWAP.Data.Migrations
{
    public partial class ModelUpdate111620215 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCharged",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCharged",
                table: "Orders");
        }
    }
}

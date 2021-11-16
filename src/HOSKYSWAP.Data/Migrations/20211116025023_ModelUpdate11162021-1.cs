using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HOSKYSWAP.Data.Migrations
{
    public partial class ModelUpdate111620211 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExecuteTxId",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecuteTxId",
                table: "Orders");
        }
    }
}

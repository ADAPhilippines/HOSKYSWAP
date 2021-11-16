using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HOSKYSWAP.Server.Worker.Migrations
{
    public partial class ModelUpdate111620212 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TxIndex",
                table: "Orders");

            migrationBuilder.AddColumn<List<int>>(
                name: "TxIndexes",
                table: "Orders",
                type: "integer[]",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TxIndexes",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "TxIndex",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

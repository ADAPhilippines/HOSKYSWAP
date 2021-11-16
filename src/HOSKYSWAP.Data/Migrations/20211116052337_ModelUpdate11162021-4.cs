using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HOSKYSWAP.Data.Migrations
{
    public partial class ModelUpdate111620214 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<long>>(
                name: "TxIndexes",
                table: "Orders",
                type: "bigint[]",
                nullable: false,
                oldClrType: typeof(List<int>),
                oldType: "integer[]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<int>>(
                name: "TxIndexes",
                table: "Orders",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(List<long>),
                oldType: "bigint[]");
        }
    }
}

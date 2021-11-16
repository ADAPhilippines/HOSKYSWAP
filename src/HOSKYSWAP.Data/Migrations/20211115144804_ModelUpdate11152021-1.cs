using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HOSKYSWAP.Data.Migrations
{
    public partial class ModelUpdate111520211 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Index",
                table: "Orders",
                newName: "TxIndex");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TxIndex",
                table: "Orders",
                newName: "Index");
        }
    }
}

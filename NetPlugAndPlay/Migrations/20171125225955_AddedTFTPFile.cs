using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace NetPlugAndPlay.Migrations
{
    public partial class AddedTFTPFile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Network",
                table: "NetworkDevices",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TFTPFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Content = table.Column<string>(nullable: true),
                    FilePath = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TFTPFiles", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TFTPFiles");

            migrationBuilder.DropColumn(
                name: "Network",
                table: "NetworkDevices");
        }
    }
}

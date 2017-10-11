using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace NetPlugAndPlay.Migrations
{
    public partial class Bob : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NetworkDeviceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkDeviceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetworkDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DomainName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Hostname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkDevices_NetworkDeviceTypes_DeviceTypeId",
                        column: x => x.DeviceTypeId,
                        principalTable: "NetworkDeviceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NetworkInterfaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InterfaceIndex = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkInterfaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkInterfaces_NetworkDeviceTypes_DeviceTypeId",
                        column: x => x.DeviceTypeId,
                        principalTable: "NetworkDeviceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NetworkDeviceLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectedToDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConnectedToInterfaceIndex = table.Column<int>(type: "int", nullable: false),
                    InterfaceIndex = table.Column<int>(type: "int", nullable: false),
                    NetworkDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkDeviceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkDeviceLinks_NetworkDevices_ConnectedToDeviceId",
                        column: x => x.ConnectedToDeviceId,
                        principalTable: "NetworkDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NetworkDeviceLinks_NetworkDevices_NetworkDeviceId",
                        column: x => x.NetworkDeviceId,
                        principalTable: "NetworkDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NetworkDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateConfigurations_NetworkDevices_NetworkDeviceId",
                        column: x => x.NetworkDeviceId,
                        principalTable: "NetworkDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateConfigurations_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateProperties_TemplateConfigurations_TemplateConfigurationId",
                        column: x => x.TemplateConfigurationId,
                        principalTable: "TemplateConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetworkDeviceLinks_ConnectedToDeviceId",
                table: "NetworkDeviceLinks",
                column: "ConnectedToDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkDeviceLinks_NetworkDeviceId",
                table: "NetworkDeviceLinks",
                column: "NetworkDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkDevices_DeviceTypeId",
                table: "NetworkDevices",
                column: "DeviceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkInterfaces_DeviceTypeId",
                table: "NetworkInterfaces",
                column: "DeviceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateConfigurations_NetworkDeviceId",
                table: "TemplateConfigurations",
                column: "NetworkDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateConfigurations_TemplateId",
                table: "TemplateConfigurations",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateProperties_TemplateConfigurationId",
                table: "TemplateProperties",
                column: "TemplateConfigurationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetworkDeviceLinks");

            migrationBuilder.DropTable(
                name: "NetworkInterfaces");

            migrationBuilder.DropTable(
                name: "TemplateProperties");

            migrationBuilder.DropTable(
                name: "TemplateConfigurations");

            migrationBuilder.DropTable(
                name: "NetworkDevices");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropTable(
                name: "NetworkDeviceTypes");
        }
    }
}

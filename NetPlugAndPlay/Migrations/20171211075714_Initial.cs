using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace NetPlugAndPlay.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NetworkDeviceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Manufacturer = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    ProductId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkDeviceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Content = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "NetworkDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DHCPRelay = table.Column<bool>(nullable: false),
                    DHCPTftpBootfile = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    DeviceTypeId = table.Column<Guid>(nullable: true),
                    DomainName = table.Column<string>(nullable: true),
                    Hostname = table.Column<string>(nullable: true),
                    IPAddress = table.Column<string>(nullable: true),
                    Network = table.Column<string>(nullable: true)
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
                    Id = table.Column<Guid>(nullable: false),
                    DeviceTypeId = table.Column<Guid>(nullable: true),
                    InterfaceIndex = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true)
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
                name: "DHCPExclusion",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    End = table.Column<string>(nullable: true),
                    NetworkDeviceId = table.Column<Guid>(nullable: true),
                    Start = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DHCPExclusion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DHCPExclusion_NetworkDevices_NetworkDeviceId",
                        column: x => x.NetworkDeviceId,
                        principalTable: "NetworkDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NetworkDeviceLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ConnectedToDeviceId = table.Column<Guid>(nullable: true),
                    ConnectedToInterfaceIndex = table.Column<int>(nullable: false),
                    InterfaceIndex = table.Column<int>(nullable: false),
                    NetworkDeviceId = table.Column<Guid>(nullable: true)
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
                    Id = table.Column<Guid>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    NetworkDeviceId = table.Column<Guid>(nullable: true),
                    TemplateId = table.Column<Guid>(nullable: true)
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
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    TemplateConfigurationId = table.Column<Guid>(nullable: true),
                    Value = table.Column<string>(nullable: true)
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
                name: "IX_DHCPExclusion_NetworkDeviceId",
                table: "DHCPExclusion",
                column: "NetworkDeviceId");

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
                name: "DHCPExclusion");

            migrationBuilder.DropTable(
                name: "NetworkDeviceLinks");

            migrationBuilder.DropTable(
                name: "NetworkInterfaces");

            migrationBuilder.DropTable(
                name: "TemplateProperties");

            migrationBuilder.DropTable(
                name: "TFTPFiles");

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

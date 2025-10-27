using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CadenceAccounting.Migrations
{
    /// <inheritdoc />
    public partial class CompleteReportSystemSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportRelationships_ReportDataSources_FromDataSourceId",
                table: "ReportRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportRelationships_ReportDataSources_ToDataSourceId",
                table: "ReportRelationships");

            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: new Guid("abfef2c0-8504-4dce-bc62-f4d074605271"));

            migrationBuilder.DeleteData(
                table: "ProjectTemplates",
                keyColumn: "Id",
                keyValue: new Guid("924da034-99fa-4c65-ba46-42f7413d2bbd"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("13fa99fa-007b-47d5-b07f-2310d61931f4"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("16778cd9-c797-43de-b851-eedef3224f88"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("283d7f47-c1ce-4e90-951d-b3f27708d682"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("2a5c36b0-52cf-4eca-bca5-cecea2e21deb"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("33ccc79d-a314-4032-a519-b0d042e2bc0e"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("3c15929b-049a-4c6f-b36c-3450346f5f17"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("5e7912e2-61c8-4bf5-9a07-99adfe5e3299"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("9c986ff3-d827-4658-be33-6a609cac4600"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("9d8020ad-5751-4a36-ad21-31ad4047ebc9"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("c977bea5-077d-4a90-9346-3b93fb82e24c"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("d08bab15-a5dc-45f5-a094-c185be439fb4"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("e79659b8-f254-40e3-bde4-e92ca19bc6de"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("ea3cfc4d-72eb-4a4f-84af-888ad61376bb"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("f5eab7e1-4b63-4820-b11c-8fd4f9c99d5e"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("f80a9c41-b4cd-48fc-b11c-f58b9372c019"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("dd62b8cc-3f57-40f7-aec6-a217a9c6580c"));

            migrationBuilder.DropColumn(
                name: "DataType",
                table: "ReportFilters");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ReportFilters");

            migrationBuilder.RenameColumn(
                name: "FieldName",
                table: "ReportFilters",
                newName: "TableName");

            migrationBuilder.RenameIndex(
                name: "IX_ReportFilters_FieldName",
                table: "ReportFilters",
                newName: "IX_ReportFilters_TableName");

            migrationBuilder.AlterColumn<int>(
                name: "Operator",
                table: "ReportFilters",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "ColumnName",
                table: "ReportFilters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LogicOperator",
                table: "ReportFilters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ReportFilters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ReportFilters",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.CreateTable(
                name: "ReportParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParameterType = table.Column<int>(type: "int", nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    LookupSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayField = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ValueField = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportParameters_ReportDefinitions_ReportId",
                        column: x => x.ReportId,
                        principalTable: "ReportDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportParameterOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActualValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportParameterOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportParameterOptions_ReportParameters_ParameterId",
                        column: x => x.ParameterId,
                        principalTable: "ReportParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "Address", "City", "CreatedAt", "Email", "LogoPath", "Name", "Phone", "State", "TaxId", "UpdatedAt", "Website", "ZipCode" },
                values: new object[] { new Guid("9fcb8a1a-2d44-4be8-9850-6aa252249a32"), null, null, new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1506), "info@cadence-consulting.com", null, "Cadence Consulting LLC", "(312) 555-0123", null, "12-3456789", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1507), null, null });

            migrationBuilder.InsertData(
                table: "ProjectTemplates",
                columns: new[] { "Id", "CommonExpenses", "CreatedAt", "DefaultBudget", "DefaultHourlyRate", "DefaultTaxType", "Description", "Name", "UpdatedAt" },
                values: new object[] { new Guid("3acc6c1d-f18c-4bc6-b6c4-d1a304f19513"), "[\"Travel\", \"Meals\", \"Supplies\", \"Equipment\", \"Software\"]", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1852), 50000.00m, 75.00m, "1099", "Template for state government contracts", "State Contract", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1853) });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "Key", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { new Guid("074073ea-35a4-41c5-b52b-84b1b25d5813"), "Company", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1680), "Company name for invoices", "CompanyName", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1681), "Cadence Consulting LLC" },
                    { new Guid("0e487064-2f9f-4e9a-9549-06baa33b6b94"), "Security", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1740), "Session timeout in minutes", "SessionTimeout", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1740), "30" },
                    { new Guid("123531e2-ca05-44cf-857a-224969418c75"), "Invoice", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1621), "Invoice number format template", "InvoiceNumberFormat", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1622), "INV-{ProjectNumber}-{Year}-{SequentialNumber}" },
                    { new Guid("16a22a91-205e-4efc-b8be-6a359f3b265b"), "Backup", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1723), "Backup location", "Backup_Location", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1724), "C:\\Backups\\CadenceAccounting" },
                    { new Guid("17ccc96c-ff6b-4ff1-b436-2a77276c3a8e"), "Email", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1697), "SMTP port for email", "SMTP_Port", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1717), "587" },
                    { new Guid("18fb6ee3-028b-43f9-9e65-cafbec95a629"), "Company", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1687), "Company email for invoices", "CompanyEmail", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1687), "info@cadence-consulting.com" },
                    { new Guid("476c3353-e640-4a13-b110-b5472cb6d747"), "Tax", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1667), "Medicare tax rate", "MedicareRate", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1668), "1.45" },
                    { new Guid("56bcae2a-997f-4bb3-99c7-29d84e3a3bcc"), "Email", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1690), "SMTP server for email", "SMTP_Server", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1691), "smtp.office365.com" },
                    { new Guid("5a729f9c-ee49-4371-a229-ead0b29b53c0"), "Tax", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1662), "Social Security tax rate", "SocialSecurityRate", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1663), "6.2" },
                    { new Guid("79e3e442-4509-4397-85e9-7731d0e763c0"), "Tax", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1657), "Illinois state tax rate", "IllinoisStateTaxRate", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1658), "4.95" },
                    { new Guid("afe160d4-d2cb-4ada-b2e6-dd5bcb57b573"), "Project", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1676), "Default hourly rate for projects", "DefaultHourlyRate", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1676), "75.00" },
                    { new Guid("b73c1b01-aebf-4322-9081-940661de0095"), "UI", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1733), "Default theme", "Theme", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1733), "Journal" },
                    { new Guid("c2f49600-0a48-4d5e-a7df-a521023c03bb"), "Tax", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1632), "Default tax type for new projects", "DefaultTaxType", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1632), "1099" },
                    { new Guid("d33b167f-1a86-4337-8a90-b7d413f4b082"), "Tax", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1653), "Federal income tax withholding rate", "FederalWithholdingRate", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1653), "15.0" },
                    { new Guid("ed16db10-e6ed-46f1-9248-097a01e0674d"), "Backup", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1720), "Backup frequency", "Backup_Frequency", new DateTime(2025, 10, 26, 20, 21, 35, 371, DateTimeKind.Utc).AddTicks(1720), "Daily" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "EmailVerified", "FirstName", "IsActive", "LastLogin", "LastName", "PasswordHash", "Role", "UpdatedAt", "Username" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2025, 10, 26, 20, 21, 35, 370, DateTimeKind.Utc).AddTicks(7444), "admin@cadence-consulting.com", true, "Admin", true, null, "User", "$2a$11$ZM7E0cujEdKwZN9ZY2n2D.yShihKRoeOG2I4S8ZUTcOYgo/5fL15e", "Admin", new DateTime(2025, 10, 26, 20, 21, 35, 370, DateTimeKind.Utc).AddTicks(7465), "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportFilters_ColumnName",
                table: "ReportFilters",
                column: "ColumnName");

            migrationBuilder.CreateIndex(
                name: "IX_ReportParameterOptions_ParameterId",
                table: "ReportParameterOptions",
                column: "ParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportParameters_ParameterName",
                table: "ReportParameters",
                column: "ParameterName");

            migrationBuilder.CreateIndex(
                name: "IX_ReportParameters_ReportId",
                table: "ReportParameters",
                column: "ReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportRelationships_ReportDataSources_FromDataSourceId",
                table: "ReportRelationships",
                column: "FromDataSourceId",
                principalTable: "ReportDataSources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportRelationships_ReportDataSources_ToDataSourceId",
                table: "ReportRelationships",
                column: "ToDataSourceId",
                principalTable: "ReportDataSources",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportRelationships_ReportDataSources_FromDataSourceId",
                table: "ReportRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportRelationships_ReportDataSources_ToDataSourceId",
                table: "ReportRelationships");

            migrationBuilder.DropTable(
                name: "ReportParameterOptions");

            migrationBuilder.DropTable(
                name: "ReportParameters");

            migrationBuilder.DropIndex(
                name: "IX_ReportFilters_ColumnName",
                table: "ReportFilters");

            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: new Guid("9fcb8a1a-2d44-4be8-9850-6aa252249a32"));

            migrationBuilder.DeleteData(
                table: "ProjectTemplates",
                keyColumn: "Id",
                keyValue: new Guid("3acc6c1d-f18c-4bc6-b6c4-d1a304f19513"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("074073ea-35a4-41c5-b52b-84b1b25d5813"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("0e487064-2f9f-4e9a-9549-06baa33b6b94"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("123531e2-ca05-44cf-857a-224969418c75"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("16a22a91-205e-4efc-b8be-6a359f3b265b"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("17ccc96c-ff6b-4ff1-b436-2a77276c3a8e"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("18fb6ee3-028b-43f9-9e65-cafbec95a629"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("476c3353-e640-4a13-b110-b5472cb6d747"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("56bcae2a-997f-4bb3-99c7-29d84e3a3bcc"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("5a729f9c-ee49-4371-a229-ead0b29b53c0"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("79e3e442-4509-4397-85e9-7731d0e763c0"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("afe160d4-d2cb-4ada-b2e6-dd5bcb57b573"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("b73c1b01-aebf-4322-9081-940661de0095"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("c2f49600-0a48-4d5e-a7df-a521023c03bb"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("d33b167f-1a86-4337-8a90-b7d413f4b082"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("ed16db10-e6ed-46f1-9248-097a01e0674d"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DropColumn(
                name: "ColumnName",
                table: "ReportFilters");

            migrationBuilder.DropColumn(
                name: "LogicOperator",
                table: "ReportFilters");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ReportFilters");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ReportFilters");

            migrationBuilder.RenameColumn(
                name: "TableName",
                table: "ReportFilters",
                newName: "FieldName");

            migrationBuilder.RenameIndex(
                name: "IX_ReportFilters_TableName",
                table: "ReportFilters",
                newName: "IX_ReportFilters_FieldName");

            migrationBuilder.AlterColumn<string>(
                name: "Operator",
                table: "ReportFilters",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "DataType",
                table: "ReportFilters",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ReportFilters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "Address", "City", "CreatedAt", "Email", "LogoPath", "Name", "Phone", "State", "TaxId", "UpdatedAt", "Website", "ZipCode" },
                values: new object[] { new Guid("abfef2c0-8504-4dce-bc62-f4d074605271"), null, null, new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(915), "info@cadence-consulting.com", null, "Cadence Consulting LLC", "(312) 555-0123", null, "12-3456789", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(917), null, null });

            migrationBuilder.InsertData(
                table: "ProjectTemplates",
                columns: new[] { "Id", "CommonExpenses", "CreatedAt", "DefaultBudget", "DefaultHourlyRate", "DefaultTaxType", "Description", "Name", "UpdatedAt" },
                values: new object[] { new Guid("924da034-99fa-4c65-ba46-42f7413d2bbd"), "[\"Travel\", \"Meals\", \"Supplies\", \"Equipment\", \"Software\"]", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1166), 50000.00m, 75.00m, "1099", "Template for state government contracts", "State Contract", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1167) });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "Key", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { new Guid("13fa99fa-007b-47d5-b07f-2310d61931f4"), "Project", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1023), "Default hourly rate for projects", "DefaultHourlyRate", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1023), "75.00" },
                    { new Guid("16778cd9-c797-43de-b851-eedef3224f88"), "Invoice", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(984), "Invoice number format template", "InvoiceNumberFormat", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(985), "INV-{ProjectNumber}-{Year}-{SequentialNumber}" },
                    { new Guid("283d7f47-c1ce-4e90-951d-b3f27708d682"), "Tax", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1013), "Illinois state tax rate", "IllinoisStateTaxRate", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1013), "4.95" },
                    { new Guid("2a5c36b0-52cf-4eca-bca5-cecea2e21deb"), "Tax", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1010), "Federal income tax withholding rate", "FederalWithholdingRate", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1011), "15.0" },
                    { new Guid("33ccc79d-a314-4032-a519-b0d042e2bc0e"), "Tax", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1018), "Medicare tax rate", "MedicareRate", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1019), "1.45" },
                    { new Guid("3c15929b-049a-4c6f-b36c-3450346f5f17"), "Tax", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1016), "Social Security tax rate", "SocialSecurityRate", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1016), "6.2" },
                    { new Guid("5e7912e2-61c8-4bf5-9a07-99adfe5e3299"), "Email", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1031), "SMTP server for email", "SMTP_Server", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1032), "smtp.office365.com" },
                    { new Guid("9c986ff3-d827-4658-be33-6a609cac4600"), "Backup", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1049), "Backup location", "Backup_Location", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1049), "C:\\Backups\\CadenceAccounting" },
                    { new Guid("9d8020ad-5751-4a36-ad21-31ad4047ebc9"), "Security", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1066), "Session timeout in minutes", "SessionTimeout", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1066), "30" },
                    { new Guid("c977bea5-077d-4a90-9346-3b93fb82e24c"), "Backup", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1038), "Backup frequency", "Backup_Frequency", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1047), "Daily" },
                    { new Guid("d08bab15-a5dc-45f5-a094-c185be439fb4"), "Company", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1026), "Company name for invoices", "CompanyName", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1027), "Cadence Consulting LLC" },
                    { new Guid("e79659b8-f254-40e3-bde4-e92ca19bc6de"), "Company", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1029), "Company email for invoices", "CompanyEmail", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1029), "info@cadence-consulting.com" },
                    { new Guid("ea3cfc4d-72eb-4a4f-84af-888ad61376bb"), "Tax", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(992), "Default tax type for new projects", "DefaultTaxType", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(992), "1099" },
                    { new Guid("f5eab7e1-4b63-4820-b11c-8fd4f9c99d5e"), "Email", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1036), "SMTP port for email", "SMTP_Port", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1036), "587" },
                    { new Guid("f80a9c41-b4cd-48fc-b11c-f58b9372c019"), "UI", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1051), "Default theme", "Theme", new DateTime(2025, 10, 25, 3, 7, 25, 691, DateTimeKind.Utc).AddTicks(1052), "Journal" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "EmailVerified", "FirstName", "IsActive", "LastLogin", "LastName", "PasswordHash", "Role", "UpdatedAt", "Username" },
                values: new object[] { new Guid("dd62b8cc-3f57-40f7-aec6-a217a9c6580c"), new DateTime(2025, 10, 25, 3, 7, 25, 690, DateTimeKind.Utc).AddTicks(9863), "admin@cadence-consulting.com", true, "Admin", true, null, "User", "$2a$11$uwZDP5h.aUXWR//WUSC5FekyhJ5vJx9dwcwp8M/P8H21sw1HnbFga", "Admin", new DateTime(2025, 10, 25, 3, 7, 25, 690, DateTimeKind.Utc).AddTicks(9875), "admin" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReportRelationships_ReportDataSources_FromDataSourceId",
                table: "ReportRelationships",
                column: "FromDataSourceId",
                principalTable: "ReportDataSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportRelationships_ReportDataSources_ToDataSourceId",
                table: "ReportRelationships",
                column: "ToDataSourceId",
                principalTable: "ReportDataSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }
    }
}

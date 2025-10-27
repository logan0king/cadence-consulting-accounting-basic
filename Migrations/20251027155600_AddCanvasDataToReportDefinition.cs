using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CadenceAccounting.Migrations
{
    /// <inheritdoc />
    public partial class AddCanvasDataToReportDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: new Guid("095d622f-6c3b-4d35-a673-53aa9d57149b"));

            migrationBuilder.DeleteData(
                table: "ProjectTemplates",
                keyColumn: "Id",
                keyValue: new Guid("40a41c00-bf19-4549-91b7-f6b8914e6e7a"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("00d4748e-63be-4745-85f7-69cf437d4b45"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("18f9e43a-4edb-4c44-b81c-b51feb0cdc16"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("1a9c840f-6805-454e-97d2-9117ffb94240"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("1c664eda-d3e9-4f76-a1b4-d1327c21b1be"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("536dd763-b7d1-4b3c-b24a-dfcf0eeb9788"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("5640461d-6f7c-477e-aa92-56eca2a0417a"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("6dad7e54-5aa3-411c-bf30-074edd85a22e"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("6fc42372-aac8-41a0-a890-506ede8b0344"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("7138ba49-c91a-4fb7-812a-bf6d7569db12"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("8137cf18-38da-4a86-a693-23ed95327593"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("8273d4be-e7bf-4c67-b348-d468b424cfcf"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("ace53a8c-2f5e-4f7a-b7dd-f7b5fb2898a5"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("c1afad1f-800b-4f60-aa9e-fd1599fb8c11"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("c728ad35-b9f7-42d1-a63d-8819c460c1bd"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("e29043a6-8115-44d5-9969-6be6d9deb395"));

            migrationBuilder.AddColumn<string>(
                name: "CanvasData",
                table: "ReportDefinitions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "Address", "City", "CreatedAt", "Email", "LogoPath", "Name", "Phone", "State", "TaxId", "UpdatedAt", "Website", "ZipCode" },
                values: new object[] { new Guid("7441147f-9dea-4278-847e-a2560f66b044"), null, null, new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5199), "info@cadence-consulting.com", null, "Cadence Consulting LLC", "(312) 555-0123", null, "12-3456789", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5200), null, null });

            migrationBuilder.InsertData(
                table: "ProjectTemplates",
                columns: new[] { "Id", "CommonExpenses", "CreatedAt", "DefaultBudget", "DefaultHourlyRate", "DefaultTaxType", "Description", "Name", "UpdatedAt" },
                values: new object[] { new Guid("a8d423e1-ba59-4430-9af2-f1404e47cc36"), "[\"Travel\", \"Meals\", \"Supplies\", \"Equipment\", \"Software\"]", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(6207), 50000.00m, 75.00m, "1099", "Template for state government contracts", "State Contract", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(6215) });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "Key", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { new Guid("0d859be8-7122-482f-9134-c844d0f52369"), "UI", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5926), "Default theme", "Theme", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5927), "Journal" },
                    { new Guid("119246c6-6a3a-45f0-93f0-b6f48dfc6756"), "Tax", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5419), "Federal income tax withholding rate", "FederalWithholdingRate", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5420), "15.0" },
                    { new Guid("2719264e-8eea-4013-b345-c747081ba222"), "Company", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5801), "Company name for invoices", "CompanyName", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5801), "Cadence Consulting LLC" },
                    { new Guid("3abf1001-13f4-4b8c-b240-1726c491149b"), "Backup", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5895), "Backup frequency", "Backup_Frequency", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5895), "Daily" },
                    { new Guid("3be9e849-8733-49a5-8637-6329a6e8aeeb"), "Email", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5814), "SMTP server for email", "SMTP_Server", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5814), "smtp.office365.com" },
                    { new Guid("4920be27-84e6-4691-8d30-0514eb316ad0"), "Tax", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5427), "Illinois state tax rate", "IllinoisStateTaxRate", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5428), "4.95" },
                    { new Guid("6c772e47-19ac-4e64-84ab-f13ffaaa346b"), "Invoice", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5351), "Invoice number format template", "InvoiceNumberFormat", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5352), "INV-{ProjectNumber}-{Year}-{SequentialNumber}" },
                    { new Guid("7a6377ce-7bf0-4ba3-bbf3-022cc2ff5fcc"), "Company", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5810), "Company email for invoices", "CompanyEmail", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5811), "info@cadence-consulting.com" },
                    { new Guid("849fc624-d124-4c7e-979e-12e2ab2b81e9"), "Tax", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5785), "Medicare tax rate", "MedicareRate", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5786), "1.45" },
                    { new Guid("92c09f99-ca2b-4654-b9b3-9d28f879272f"), "Project", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5797), "Default hourly rate for projects", "DefaultHourlyRate", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5798), "75.00" },
                    { new Guid("9cdb87da-d691-43fa-ae29-23c42fb2ea3f"), "Tax", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5379), "Default tax type for new projects", "DefaultTaxType", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5380), "1099" },
                    { new Guid("afb4e13b-fae3-4c78-82d8-5001dd3c6070"), "Tax", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5781), "Social Security tax rate", "SocialSecurityRate", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5782), "6.2" },
                    { new Guid("da3bbd78-5f50-4874-a7e8-6f397d83208f"), "Security", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5934), "Session timeout in minutes", "SessionTimeout", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5935), "30" },
                    { new Guid("dc7670b5-79d6-43df-ba08-094282bc951c"), "Email", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5822), "SMTP port for email", "SMTP_Port", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5891), "587" },
                    { new Guid("e474a56e-3b21-4a82-8a53-33807174c1c2"), "Backup", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5904), "Backup location", "Backup_Location", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(5905), "C:\\Backups\\CadenceAccounting" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(2114), "$2a$11$/k6hHg7HTI4uq.q/k0jNzuf6ommYP1ScSaCk63yrpqmuZIIy3bDhK", new DateTime(2025, 10, 27, 15, 55, 59, 407, DateTimeKind.Utc).AddTicks(2140) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: new Guid("7441147f-9dea-4278-847e-a2560f66b044"));

            migrationBuilder.DeleteData(
                table: "ProjectTemplates",
                keyColumn: "Id",
                keyValue: new Guid("a8d423e1-ba59-4430-9af2-f1404e47cc36"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("0d859be8-7122-482f-9134-c844d0f52369"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("119246c6-6a3a-45f0-93f0-b6f48dfc6756"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("2719264e-8eea-4013-b345-c747081ba222"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("3abf1001-13f4-4b8c-b240-1726c491149b"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("3be9e849-8733-49a5-8637-6329a6e8aeeb"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("4920be27-84e6-4691-8d30-0514eb316ad0"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("6c772e47-19ac-4e64-84ab-f13ffaaa346b"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("7a6377ce-7bf0-4ba3-bbf3-022cc2ff5fcc"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("849fc624-d124-4c7e-979e-12e2ab2b81e9"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("92c09f99-ca2b-4654-b9b3-9d28f879272f"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("9cdb87da-d691-43fa-ae29-23c42fb2ea3f"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("afb4e13b-fae3-4c78-82d8-5001dd3c6070"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("da3bbd78-5f50-4874-a7e8-6f397d83208f"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("dc7670b5-79d6-43df-ba08-094282bc951c"));

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("e474a56e-3b21-4a82-8a53-33807174c1c2"));

            migrationBuilder.DropColumn(
                name: "CanvasData",
                table: "ReportDefinitions");

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "Address", "City", "CreatedAt", "Email", "LogoPath", "Name", "Phone", "State", "TaxId", "UpdatedAt", "Website", "ZipCode" },
                values: new object[] { new Guid("095d622f-6c3b-4d35-a673-53aa9d57149b"), null, null, new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6403), "info@cadence-consulting.com", null, "Cadence Consulting LLC", "(312) 555-0123", null, "12-3456789", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6405), null, null });

            migrationBuilder.InsertData(
                table: "ProjectTemplates",
                columns: new[] { "Id", "CommonExpenses", "CreatedAt", "DefaultBudget", "DefaultHourlyRate", "DefaultTaxType", "Description", "Name", "UpdatedAt" },
                values: new object[] { new Guid("40a41c00-bf19-4549-91b7-f6b8914e6e7a"), "[\"Travel\", \"Meals\", \"Supplies\", \"Equipment\", \"Software\"]", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6748), 50000.00m, 75.00m, "1099", "Template for state government contracts", "State Contract", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6749) });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "Key", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { new Guid("00d4748e-63be-4745-85f7-69cf437d4b45"), "Tax", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6484), "Federal income tax withholding rate", "FederalWithholdingRate", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6484), "15.0" },
                    { new Guid("18f9e43a-4edb-4c44-b81c-b51feb0cdc16"), "Invoice", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6474), "Invoice number format template", "InvoiceNumberFormat", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6474), "INV-{ProjectNumber}-{Year}-{SequentialNumber}" },
                    { new Guid("1a9c840f-6805-454e-97d2-9117ffb94240"), "Email", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6510), "SMTP server for email", "SMTP_Server", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6510), "smtp.office365.com" },
                    { new Guid("1c664eda-d3e9-4f76-a1b4-d1327c21b1be"), "Backup", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6532), "Backup location", "Backup_Location", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6532), "C:\\Backups\\CadenceAccounting" },
                    { new Guid("536dd763-b7d1-4b3c-b24a-dfcf0eeb9788"), "Security", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6541), "Session timeout in minutes", "SessionTimeout", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6542), "30" },
                    { new Guid("5640461d-6f7c-477e-aa92-56eca2a0417a"), "Project", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6500), "Default hourly rate for projects", "DefaultHourlyRate", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6500), "75.00" },
                    { new Guid("6dad7e54-5aa3-411c-bf30-074edd85a22e"), "Tax", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6480), "Default tax type for new projects", "DefaultTaxType", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6480), "1099" },
                    { new Guid("6fc42372-aac8-41a0-a890-506ede8b0344"), "UI", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6534), "Default theme", "Theme", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6534), "Journal" },
                    { new Guid("7138ba49-c91a-4fb7-812a-bf6d7569db12"), "Company", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6502), "Company name for invoices", "CompanyName", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6503), "Cadence Consulting LLC" },
                    { new Guid("8137cf18-38da-4a86-a693-23ed95327593"), "Tax", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6491), "Social Security tax rate", "SocialSecurityRate", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6491), "6.2" },
                    { new Guid("8273d4be-e7bf-4c67-b348-d468b424cfcf"), "Backup", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6514), "Backup frequency", "Backup_Frequency", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6528), "Daily" },
                    { new Guid("ace53a8c-2f5e-4f7a-b7dd-f7b5fb2898a5"), "Tax", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6493), "Medicare tax rate", "MedicareRate", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6494), "1.45" },
                    { new Guid("c1afad1f-800b-4f60-aa9e-fd1599fb8c11"), "Company", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6507), "Company email for invoices", "CompanyEmail", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6508), "info@cadence-consulting.com" },
                    { new Guid("c728ad35-b9f7-42d1-a63d-8819c460c1bd"), "Tax", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6486), "Illinois state tax rate", "IllinoisStateTaxRate", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6486), "4.95" },
                    { new Guid("e29043a6-8115-44d5-9969-6be6d9deb395"), "Email", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6512), "SMTP port for email", "SMTP_Port", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(6512), "587" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(5437), "$2a$11$XWPgQyLI1DjSpXuGgaFCreYCYsIGpTiw7jj8dW47Ko6OxaM28QW.e", new DateTime(2025, 10, 26, 20, 23, 12, 776, DateTimeKind.Utc).AddTicks(5443) });
        }
    }
}

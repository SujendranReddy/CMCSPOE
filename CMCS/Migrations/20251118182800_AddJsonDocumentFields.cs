using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMCS.Migrations
{
    /// <inheritdoc />
    public partial class AddJsonDocumentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OriginalDocuments",
                table: "Claims",
                newName: "OriginalDocumentsJson");

            migrationBuilder.RenameColumn(
                name: "EncryptedDocuments",
                table: "Claims",
                newName: "EncryptedDocumentsJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OriginalDocumentsJson",
                table: "Claims",
                newName: "OriginalDocuments");

            migrationBuilder.RenameColumn(
                name: "EncryptedDocumentsJson",
                table: "Claims",
                newName: "EncryptedDocuments");
        }
    }
}

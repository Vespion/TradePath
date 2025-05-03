using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace TradePath.DataStore.Database.Migrations
{
	/// <inheritdoc />
	public partial class Initial : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterDatabase()
				.Annotation("Sqlite:InitSpatialMetaData", true);

			migrationBuilder.CreateTable(
				name: "Systems",
				columns: table => new
				{
					Id = table.Column<int>(type: "INTEGER", nullable: false),
					Name = table.Column<string>(type: "TEXT", nullable: false),
					Position = table.Column<Point>(type: "POINTZ", nullable: false),
					LastModified = table.Column<DateTimeOffset>(type: "TEXT", rowVersion: true, nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Systems", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Star",
				columns: table => new
				{
					Id = table.Column<int>(type: "INTEGER", nullable: false),
					DistanceFromPrimary = table.Column<float>(type: "REAL", nullable: true),
					CanScoop = table.Column<bool>(type: "INTEGER", nullable: false),
					CanBoost = table.Column<bool>(type: "INTEGER", nullable: false),
					SystemId = table.Column<int>(type: "INTEGER", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Star", x => x.Id);
					table.ForeignKey(
						name: "FK_Star_Systems_SystemId",
						column: x => x.SystemId,
						principalTable: "Systems",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Stations",
				columns: table => new
				{
					Id = table.Column<int>(type: "INTEGER", nullable: false),
					Name = table.Column<string>(type: "TEXT", nullable: false),
					DistanceFromPrimary = table.Column<float>(type: "REAL", nullable: false),
					MaxPadSize = table.Column<byte>(type: "INTEGER", nullable: false),
					IsPermanent = table.Column<bool>(type: "INTEGER", nullable: false),
					IsPlanetary = table.Column<bool>(type: "INTEGER", nullable: false),
					IsPlayerOwned = table.Column<bool>(type: "INTEGER", nullable: false),
					SystemId = table.Column<int>(type: "INTEGER", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Stations", x => x.Id);
					table.ForeignKey(
						name: "FK_Stations_Systems_SystemId",
						column: x => x.SystemId,
						principalTable: "Systems",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.Sql(
				"""
				CREATE VIRTUAL TABLE SystemNameIndex USING fts5(Name, content='Systems', content_rowid='Id', tokenize="trigram remove_diacritics 1");
				CREATE TRIGGER SystemNameIndex_ai AFTER INSERT ON Systems BEGIN
					INSERT INTO SystemNameIndex(rowid, Name) VALUES (new.Id, new.Name);
				END;
				CREATE TRIGGER SystemNameIndex_ad AFTER DELETE ON Systems BEGIN
					INSERT INTO SystemNameIndex(SystemNameIndex, rowid, Name) VALUES('delete', old.Id, old.Name);
				END;
				CREATE TRIGGER SystemNameIndex_au AFTER UPDATE ON Systems BEGIN
					INSERT INTO SystemNameIndex(SystemNameIndex, rowid, Name) VALUES('delete', old.Id, old.Name);
					INSERT INTO SystemNameIndex(rowid, Name) VALUES (new.Id, new.Name);
				END;
				"""
			);
			
			migrationBuilder.Sql(
				"""
				CREATE VIRTUAL TABLE StationNameIndex USING fts5(Name, content='Stations', content_rowid='Id', tokenize="trigram remove_diacritics 1");
				CREATE TRIGGER StationNameIndex_ai AFTER INSERT ON Stations BEGIN
					INSERT INTO StationNameIndex(rowid, Name) VALUES (new.Id, new.Name);
				END;
				CREATE TRIGGER StationNameIndex_ad AFTER DELETE ON Stations BEGIN
					INSERT INTO StationNameIndex(StationNameIndex, rowid, Name) VALUES('delete', old.Id, old.Name);
				END;
				CREATE TRIGGER StationNameIndex_au AFTER UPDATE ON Stations BEGIN
					INSERT INTO StationNameIndex(StationNameIndex, rowid, Name) VALUES('delete', old.Id, old.Name);
					INSERT INTO StationNameIndex(rowid, Name) VALUES (new.Id, new.Name);
				END;
				"""
			);

			migrationBuilder.CreateIndex(
				name: "IX_Star_SystemId",
				table: "Star",
				column: "SystemId");

			migrationBuilder.CreateIndex(
				name: "IX_Stations_SystemId",
				table: "Stations",
				column: "SystemId");

			migrationBuilder.Sql("PRAGMA optimize;");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Star");

			migrationBuilder.DropTable(
				name: "StationNameIndex");

			migrationBuilder.DropTable(
				name: "SystemNameIndex");

			migrationBuilder.DropTable(
				name: "Stations");

			migrationBuilder.DropTable(
				name: "Systems");
		}
	}
}

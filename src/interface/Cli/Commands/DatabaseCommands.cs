using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using TradePath.Cli.Binders;
using TradePath.DataStore.Database;

namespace TradePath.Cli.Commands;

internal static class DatabaseCommands
{
	public static Command Build()
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		var cmd = new Command(Resources.Commands.Database_Name, Resources.Commands.Database_Description)
		{
			BuildMigrateCommand()
		};
		cmd.AddGlobalOption(GlobalOptions.WorkingDirectory);
		return cmd;
	}

	private static Command BuildMigrateCommand()
	{
		using var buildAct = Telemetry.ActivitySource.StartActivityWithParent();
		var migrateCommand = new Command(
			Resources.Commands.Database_Migrate_Name,
			Resources.Commands.Database_Migrate_Description);

		var migrationTargetOption = new Option<string?>(
			Resources.Commands.Database_Migrate_Options_Target_Name.Split('|'),
			Resources.Commands.Database_Migrate_Options_Target_Description)
		{
			IsRequired = false,
			IsHidden = true
		};

		migrateCommand.SetHandler(HandleMigrateCommand, migrationTargetOption, new GalDataContextBinder());

		return migrateCommand;
	}

	private static void HandleMigrateCommand(string? target, GalDataContext galData)
	{
		using var act = Telemetry.ActivitySource.StartActivityWithParent();
		act?.AddTag("db.migration", target ?? "latest");
		galData.Database.Migrate(target);
	}
}

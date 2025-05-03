using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using TradePath.Cli.Binders;

namespace TradePath.Cli.Commands;

internal static class DatabaseCommands
{
	public static Command Build()
	{
		var cmd = new Command(Resources.Commands.Database_Name, Resources.Commands.Database_Description)
		{
			BuildMigrateCommand()
		};
		cmd.AddGlobalOption(GlobalOptions.WorkingDirectory);
		return cmd;
	}

	private static Command BuildMigrateCommand()
	{
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
		
		migrateCommand.SetHandler((target, galData) =>
		{
			galData.Database.Migrate(target);
		}, migrationTargetOption, new GalDataContextBinder());

		return migrateCommand;
	}
}

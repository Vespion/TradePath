using Validly;
using Vogen;

namespace TradePath.Models;

[Validatable]
public partial class Station
{
	public required StationId Id { get; init; }

	public required StationName Name { get; init; }

	public required float DistanceFromPrimary { get; set; }

	public required PadSize MaxPadSize { get; set; }

	public required bool IsPermanent { get; set; }

	public required bool IsPlanetary { get; set; }

	public required bool IsPlayerOwned { get; set; }


	public required StarSystemId SystemId { get; init; }

	public StarSystem? System { get; init; } = null!;
}

public enum PadSize : byte
{
	Small,
	Medium,
	Large
}

[ValueObject<string>]
public partial record StationName;

[ValueObject<int>]
public partial record StationId;

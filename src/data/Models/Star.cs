using Validly;
using Vogen;

namespace TradePath.Models;

[Validatable]
public partial class Star
{
	public required StarId Id { get; set; }
	
	public required float? DistanceFromPrimary { get; set; }
	
	public required bool CanScoop { get; set; }
	
	public required bool CanBoost { get; set; }
	
	public StarSystem? System { get; set; } = null!;
	
	public required StarSystemId SystemId { get; set; }
}

[ValueObject<int>]
public partial record StarId;

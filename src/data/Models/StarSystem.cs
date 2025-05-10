using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;
using TradePath.Models.Validation;
using Validly;
using Validly.Extensions.Validators.Collections;
using Validly.Extensions.Validators.Common;
using Validly.Extensions.Validators.Numbers;
using Vogen;

namespace TradePath.Models;

[Validatable(NoAutoValidators = true)]
public partial class StarSystem
{
	[Validly.Extensions.Validators.Common.Required]
	[GreaterThan(0)]
	public required StarSystemId Id { get; set; }

	[Validly.Extensions.Validators.Common.Required]
	[NotEmpty]
	public required StarSystemName Name { get; set; }

	[Validly.Extensions.Validators.Common.Required]
	[ZCoordinate]
	public required Point Position { get; set; }

	[ConcurrencyCheck]
	[Validly.Extensions.Validators.Common.Required]
	public required DateTimeOffset LastModified { get; set; }

	[Validly.Validators.CustomValidation]
	[Validly.Extensions.Validators.Common.Required]
	[MinCollectionSize(1)]
	public required ICollection<Star> Stars { get; set; }


	/// <inheritdoc />
	IEnumerable<ValidationMessage> IStarSystemCustomValidation.ValidateStars()
	{
		if (Stars.Count(s => s.DistanceFromPrimary == null) != 1)
		{
			yield return new ValidationMessage(
				"There must be exactly one primary star set by having it's distance property set to null.",
				"DataStore.Validation.StarSystem.MultiplePrimaryStars"
			);
		}
	}
}

[ValueObject<string>(toPrimitiveCasting: CastOperator.Implicit)]
public partial record StarSystemName;

[ValueObject<int>]
public partial record StarSystemId;

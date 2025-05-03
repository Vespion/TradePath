using NetTopologySuite.Geometries;
using Validly;
using Validly.Validators;

namespace TradePath.Models.Validation;

[Validator]
[ValidatorDescription("must be a coordinate with valid XYZ ordinates and no M ordinate.")]
[AttributeUsage(AttributeTargets.Property)]
public class ZCoordinateAttribute : Attribute
{
	private static readonly ValidationMessage InvalidCoordinateMessage = new(
		"Position is not valid. It must be a valid coordinate.",
		"Validation.DataStore.ZCoordinate.Invalid"
	);
	
	private static readonly ValidationMessage MissingZMessage = new(
		"Position is not valid. Z Ordinate is missing.",
		"Validation.DataStore.ZCoordinate.MissingZ"
	);
	
	private static readonly ValidationMessage MSetMessage = new(
		"Position is not valid. M Ordinate must not be set.",
		"Validation.DataStore.ZCoordinate.MSet"
	);

	public ValidationMessage? IsValid<T>(T value)
		where T : Point
	{
		if (!value.IsValid)
		{
			return InvalidCoordinateMessage;
		}

		if (double.IsNaN(value.Z))
		{
			return MissingZMessage;
		}
		
		if (!double.IsNaN(value.M))
		{
			return MSetMessage;
		}

		return null;
	}
}

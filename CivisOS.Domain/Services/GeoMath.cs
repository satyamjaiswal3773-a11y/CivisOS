using CivisOS.Domain.Entities;

namespace CivisOS.Domain.Services;

/// <summary>
/// Great-circle (Haversine) helpers for geo-fence checks.
/// </summary>
public static class GeoMath
{
    private const double EarthRadiusMeters = 6_371_000d;

    /// <summary>
    /// Distance in meters between two WGS84 points using the Haversine formula.
    /// </summary>
    public static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var φ1 = DegreesToRadians(lat1);
        var φ2 = DegreesToRadians(lat2);
        var Δφ = DegreesToRadians(lat2 - lat1);
        var Δλ = DegreesToRadians(lng2 - lng1);

        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2)
                + Math.Cos(φ1) * Math.Cos(φ2)
                * Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Returns true when the point is within (or on) the fence radius of the center.
    /// </summary>
    public static bool IsInsideFence(GeoFence fence, double latitude, double longitude)
        => IsInsideFence(fence.CenterLatitude, fence.CenterLongitude, fence.RadiusMeters, latitude, longitude);

    public static bool IsInsideFence(
        double centerLatitude,
        double centerLongitude,
        double radiusMeters,
        double latitude,
        double longitude)
    {
        if (radiusMeters < 0)
        {
            return false;
        }

        return DistanceMeters(centerLatitude, centerLongitude, latitude, longitude) <= radiusMeters;
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180d);
}

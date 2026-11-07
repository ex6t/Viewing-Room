using System;
using System.Globalization;

public static class SeasonsCalendar
{
    public const int MilestoneCount = 4;
    const double DegreesToRadians = Math.PI / 180.0;
    const double RadiansToHours = 12.0 / Math.PI;
    static readonly string[] MilestoneNames = { "March equinox", "June solstice", "September equinox", "December solstice" };
    static readonly DateTime TeachingPerihelion = new DateTime(2001, 1, 3);
    static readonly double eccentricity;
    static readonly double springAnomaly;
    static readonly double[,] equatorialFrame;
    static readonly double initialSolarRightAscension;

    static SeasonsCalendar()
    {
        Bergers.BergerSol.CalculateOrbitalParameters(EarthOrbitModel.ReferenceYear,
            out eccentricity, out double obliquity, out double perihelionLongitude);
        //same vernal reference and equatorial frame as the California sky
        springAnomaly = Wrap(180.0 - perihelionLongitude, 360.0);
        GenerateRotation.GenerateRot.rotation(obliquity, springAnomaly * DegreesToRadians, out equatorialFrame);
        initialSolarRightAscension = SolarRightAscension(0.0);
    }

    public static string MilestoneName(int index) { return MilestoneNames[index]; }
    public static double MilestoneDays(int index) { return DaysAtSolarLongitude(index * 90.0); }

    public static double DaysAtSolarLongitude(double longitudeDegrees)
    {
        return DaysAtTrueAnomaly(TrueAnomalyAtSolarLongitude(longitudeDegrees));
    }

    public static double SolarLongitudeAtDays(double days)
    {
        return Wrap(TrueAnomalyAtDays(days) - springAnomaly, 360.0);
    }

    public static double TrueAnomalyAtSolarLongitude(double longitudeDegrees)
    {
        return Wrap(springAnomaly + longitudeDegrees, 360.0);
    }

    public static double DaysAtTrueAnomaly(double anomalyDegrees)
    {
        Keplerian.KeplerianSolver.keplerian(EarthOrbitModel.PeriodDays, eccentricity,
            Wrap(anomalyDegrees, 360.0), out double days, out _);
        return days;
    }

    public static double TrueAnomalyAtDays(double days)
    {
        double meanAnomaly = Wrap(days, EarthOrbitModel.PeriodDays) / EarthOrbitModel.PeriodDays * 360.0;
        Keplerian.KeplerianSolver.keplerian_inverse(eccentricity, meanAnomaly, out double anomaly);
        return anomaly;
    }

    public static string DateLabel(double days)
    {
        //day zero is 14:00 at perihelion, so roll the label at the observer's solar midnight
        double orbitalDay = Wrap(days, EarthOrbitModel.PeriodDays);
        double solarHours = WrappedSolarMinutes(SolarHoursAtDays(days)) / 60.0;
        double dayIndex = Math.Round(orbitalDay + (EarthObservationModel.InitialSolarHours - solarHours) / 24.0);
        return "Approx. " + TeachingPerihelion.AddDays(dayIndex).ToString("MMM d", CultureInfo.InvariantCulture);
    }

    public static int SolarClockMinutes(double solarHours) { return (int)WrappedSolarMinutes(solarHours); }

    public static double WithSolarTime(double orbitalDays, double solarHours)
    {
        //find the nearest occurrence of this apparent solar time, including the small seasonal drift
        double days = orbitalDays;
        for (int i = 0; i < 4; i++)
        {
            double hourDifference = Wrap(solarHours - SolarHoursAtDays(days) + 12.0, 24.0) - 12.0;
            days += hourDifference / 24.0;
        }
        return days;
    }

    public static double SolarHoursAtDays(double days)
    {
        double solarRightAscension = SolarRightAscension(TrueAnomalyAtDays(days));
        double siderealPhase = (days * EarthObservationModel.SiderealTurnsPerDay) % 1.0;
        double solarHour = EarthObservationModel.InitialSolarHours + siderealPhase * 24.0 +
            (initialSolarRightAscension - solarRightAscension) * RadiansToHours;
        return Wrap(solarHour, 24.0);
    }

    static double SolarRightAscension(double anomalyDegrees)
    {
        double radians = anomalyDegrees * DegreesToRadians;
        double sunX = -Math.Cos(radians), sunY = -Math.Sin(radians);
        return Math.Atan2(sunX * equatorialFrame[1, 0] + sunY * equatorialFrame[1, 1],
            sunX * equatorialFrame[0, 0] + sunY * equatorialFrame[0, 1]);
    }

    static double WrappedSolarMinutes(double solarHours)
    {
        //keep exact minute boundaries stable against floating point drift
        return Wrap(solarHours * 60.0 + 0.000001, 1440.0);
    }

    static double Wrap(double value, double period)
    {
        double remainder = value % period;
        return remainder < 0.0 ? remainder + period : remainder;
    }
}

using System;

namespace Seasons
{
    public static class SeasonHelper
    {
        public enum SeasonType
        {
            Winter,
            Spring,
            Summer,
            Fall
        }

        public static SeasonType GetSeasonForTime(
            double timeInYear,
            double T,
            double winter_len,
            double spring_len,
            double summer_len,
            double fall_len)
        {
            double t = ((timeInYear % T) + T) % T;

            double spring_start = winter_len;
            double summer_start = winter_len + spring_len;
            double fall_start = winter_len + spring_len + summer_len;

            if (t < spring_start) return SeasonType.Winter;
            if (t < summer_start) return SeasonType.Spring;
            if (t < fall_start) return SeasonType.Summer;
            return SeasonType.Fall;
        }
    }
}
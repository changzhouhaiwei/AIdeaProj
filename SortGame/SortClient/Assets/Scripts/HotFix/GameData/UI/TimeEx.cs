using System;

public static class TimeEx
{
    public static DateTime GetTimeStartByType(this DateTime now, string TimeType)
    {
        switch (TimeType)
        {
            case "Week":
                return now.AddDays(-(int)now.DayOfWeek + 1);

            case "Month":
                return now.AddDays(-now.Day + 1);

            case "Season":
                var time = now.AddMonths(0 - ((now.Month - 1) % 3));
                return time.AddDays(-time.Day + 1);

            case "Year":
                return now.AddDays(-now.DayOfYear + 1);

            default:
                return now;
        }
    }

    public static DateTime GetTimeEndByType(this DateTime now, string TimeType)
    {
        switch (TimeType)
        {
            case "Week":
                return now.AddDays(7 - (int)now.DayOfWeek);

            case "Month":
                return now.AddMonths(1).AddDays(-now.AddMonths(1).Day + 1).AddDays(-1);

            case "Season":
                var time = now.AddMonths((3 - ((now.Month - 1) % 3) - 1));
                return time.AddMonths(1).AddDays(-time.AddMonths(1).Day + 1).AddDays(-1);

            case "Year":
                var time2 = now.AddYears(1);
                return time2.AddDays(-time2.DayOfYear);

            default:
                return now;
        }
    }
}
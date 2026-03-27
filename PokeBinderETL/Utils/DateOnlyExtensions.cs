using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.ETL.Utils;

public static class DateOnlyExtensions
{
    public static long ToUnixTimeSeconds(this DateOnly date, TimeZoneInfo? timeZone = null)
    {
        timeZone ??= TimeZoneInfo.Utc;

        var dateTime = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, timeZone);

        return new DateTimeOffset(utcDateTime).ToUnixTimeSeconds();
    }
}

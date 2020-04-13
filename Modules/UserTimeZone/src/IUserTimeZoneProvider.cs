namespace UserTimeZone
{
    using System;
    using System.Threading.Tasks;

    public interface IUserTimeZoneProvider
    {
        Task<TimeZoneInfo> GetTimeZone(ulong guildId, ulong userId);

        Task SetTimeZone(ulong guildId, ulong userId, TimeZoneInfo value);
    }
}
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ManagementGame.Domain;

public static class Numbers
{
    public static long Divide(long numerator, long denominator)
    {
        if (denominator <= 0) throw new RuleViolation("Divisor must be positive.");
        return checked((long)decimal.Round((decimal)numerator / denominator, 0, MidpointRounding.ToEven));
    }
}

public static class KeyedRandom
{
    public const string Version = "rng-v1";
    public static ulong Draw(ulong seed, string domain, string eventId, string purpose, int index = 0, int retry = 0)
    {
        var fields = new[] { Version, seed.ToString(CultureInfo.InvariantCulture), domain, eventId, purpose,
            index.ToString(CultureInfo.InvariantCulture), retry.ToString(CultureInfo.InvariantCulture) };
        var bytes = new List<byte>();
        foreach (var field in fields)
        {
            var data = Encoding.UTF8.GetBytes(field.Normalize(NormalizationForm.FormC));
            var length = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(length, checked((uint)data.Length));
            bytes.AddRange(length); bytes.AddRange(data);
        }
        return BinaryPrimitives.ReadUInt64BigEndian(SHA256.HashData(bytes.ToArray()));
    }
    public static int Range(ulong seed, string domain, string eventId, string purpose, int count)
    {
        if (count <= 0) throw new RuleViolation("Random range must be positive.");
        var range = (ulong)count;
        var threshold = unchecked(0UL - range) % range;
        for (var retry = 0; ; retry++)
        {
            var draw = Draw(seed, domain, eventId, purpose, 0, retry);
            if (draw >= threshold) return (int)(draw % range);
        }
    }
}

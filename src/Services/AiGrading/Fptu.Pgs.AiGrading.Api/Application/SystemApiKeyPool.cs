namespace Fptu.Pgs.AiGrading.Api.Application;

public interface ISystemApiKeyPool
{
    int Count { get; }

    IReadOnlyList<string> GetCandidates();
}

public sealed class SystemApiKeyPool : ISystemApiKeyPool
{
    private readonly string[] _keys;
    private int _nextStartIndex = -1;

    public SystemApiKeyPool(IConfiguration configuration)
    {
        _keys = BuildKeyList(configuration).ToArray();
    }

    public int Count => _keys.Length;

    public IReadOnlyList<string> GetCandidates()
    {
        if (_keys.Length == 0)
        {
            return [];
        }

        var startIndex = (int)(
            (uint)Interlocked.Increment(ref _nextStartIndex) %
            (uint)_keys.Length);
        var orderedKeys = new string[_keys.Length];

        for (var offset = 0; offset < _keys.Length; offset++)
        {
            orderedKeys[offset] = _keys[(startIndex + offset) % _keys.Length];
        }

        return orderedKeys;
    }

    private static IEnumerable<string> BuildKeyList(IConfiguration configuration)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in EnumerateConfiguredKeys(configuration))
        {
            var normalizedKey = key.Trim();
            if (normalizedKey.Length > 0 && seen.Add(normalizedKey))
            {
                yield return normalizedKey;
            }
        }
    }

    private static IEnumerable<string> EnumerateConfiguredKeys(
        IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration["GOOGLE_API_KEY"]))
        {
            yield return configuration["GOOGLE_API_KEY"]!;
        }

        foreach (var key in SplitKeys(configuration["GOOGLE_API_KEYS"]))
        {
            yield return key;
        }

        // Backward-compatible appsettings/secret configuration.
        if (!string.IsNullOrWhiteSpace(configuration["AiProvider:ApiKey"]))
        {
            yield return configuration["AiProvider:ApiKey"]!;
        }

        foreach (var key in SplitKeys(configuration["AiProvider:ApiKeys"]))
        {
            yield return key;
        }
    }

    private static IEnumerable<string> SplitKeys(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(
                [',', ';', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);
}

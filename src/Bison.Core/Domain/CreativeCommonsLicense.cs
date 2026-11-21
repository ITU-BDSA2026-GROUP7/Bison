namespace Bison.Core.Domain;

// One of the Creative Commons licenses (https://creativecommons.org/licenses/)
public sealed record CreativeCommonsLicense(string Code, string Name, string Url)
{
    public static readonly IReadOnlyList<CreativeCommonsLicense> All = new List<CreativeCommonsLicense>
    {
        new("by-4.0", "CC BY 4.0 (Attribution)",
            "https://creativecommons.org/licenses/by/4.0/"),
        new("by-sa-4.0", "CC BY-SA 4.0 (Attribution-ShareAlike)",
            "https://creativecommons.org/licenses/by-sa/4.0/"),
        new("by-nd-4.0", "CC BY-ND 4.0 (Attribution-NoDerivatives)",
            "https://creativecommons.org/licenses/by-nd/4.0/"),
        new("by-nc-4.0", "CC BY-NC 4.0 (Attribution-NonCommercial)",
            "https://creativecommons.org/licenses/by-nc/4.0/"),
        new("by-nc-sa-4.0", "CC BY-NC-SA 4.0 (Attribution-NonCommercial-ShareAlike)",
            "https://creativecommons.org/licenses/by-nc-sa/4.0/"),
        new("by-nc-nd-4.0", "CC BY-NC-ND 4.0 (Attribution-NonCommercial-NoDerivatives)",
            "https://creativecommons.org/licenses/by-nc-nd/4.0/"),
        new("cc0-1.0", "CC0 1.0 (Public Domain Dedication)",
            "https://creativecommons.org/publicdomain/zero/1.0/"),
    };

    public const int MaxCodeLength = 20;

    public static CreativeCommonsLicense? Find(string? code) =>
        string.IsNullOrEmpty(code) ? null : All.FirstOrDefault(l => l.Code == code);
}
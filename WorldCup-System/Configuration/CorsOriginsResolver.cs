using Microsoft.Extensions.Configuration;

namespace WorldCup_System.Configuration
{
    /// <summary>
    /// Resolves SPA CORS allow-list from configuration.
    /// Supports JSON array (<c>Cors:AllowedOrigins</c>) or a single semicolon/comma-separated string
    /// (env <c>Cors__AllowedOrigins</c>). Origins are trimmed; trailing slashes are removed.
    /// </summary>
    public static class CorsOriginsResolver
    {
        public const string PolicyName = "Spa";

        public const string DefaultOrigin = "http://localhost:4200";

        public static string[] Resolve(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            List<string> origins = new List<string>();
            IConfigurationSection section = configuration.GetSection("Cors:AllowedOrigins");
            List<IConfigurationSection> children = section.GetChildren().ToList();

            // Prefer scalar section.Value (e.g. env Cors__AllowedOrigins=…) over indexed children.
            // ASP.NET Core keeps JSON array indices when a scalar env override is added on the same key;
            // reading children first would silently ignore the production override.
            if (!string.IsNullOrWhiteSpace(section.Value))
            {
                string[] parts = section.Value.Split(
                    new[] { ';', ',' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (string part in parts)
                {
                    AddNormalized(origins, part);
                }
            }
            else if (children.Count > 0)
            {
                foreach (IConfigurationSection child in children)
                {
                    AddNormalized(origins, child.Value);
                }
            }

            if (origins.Count == 0)
            {
                origins.Add(DefaultOrigin);
            }

            return origins
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void AddNormalized(List<string> origins, string? origin)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                return;
            }

            string normalized = origin.Trim().TrimEnd('/');
            if (normalized.Length == 0)
            {
                return;
            }

            if (normalized == "*")
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins must not contain '*'. List explicit SPA origin(s) with no trailing slash.");
            }

            origins.Add(normalized);
        }
    }
}

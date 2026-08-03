using Microsoft.Extensions.Configuration;
using WorldCup_System.Configuration;

namespace WorldCup_System.Tests.Configuration
{
    public class CorsOriginsResolverTests
    {
        [Fact]
        public void Resolve_WhenSectionMissing_ReturnsDefaultLocalhostOrigin()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>());

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { CorsOriginsResolver.DefaultOrigin }, origins);
        }

        [Fact]
        public void Resolve_WhenScalarWhitespaceOnly_ReturnsDefaultLocalhostOrigin()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "   "
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { CorsOriginsResolver.DefaultOrigin }, origins);
        }

        [Fact]
        public void Resolve_WhenBlankOnlyEntries_ReturnsDefaultLocalhostOrigin()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "  ;  ,  "
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { CorsOriginsResolver.DefaultOrigin }, origins);
        }

        [Fact]
        public void Resolve_WhenBlankOnlyJsonArrayChildren_ReturnsDefaultLocalhostOrigin()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "  ",
                ["Cors:AllowedOrigins:1"] = "/"
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { CorsOriginsResolver.DefaultOrigin }, origins);
        }

        [Fact]
        public void Resolve_WhenJsonArray_ReturnsConfiguredOrigins()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.example.com",
                ["Cors:AllowedOrigins:1"] = "https://www.example.com"
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { "https://app.example.com", "https://www.example.com" }, origins);
        }

        [Fact]
        public void Resolve_WhenSemicolonSeparatedString_SplitsOrigins()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://app.example.com; https://admin.example.com"
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { "https://app.example.com", "https://admin.example.com" }, origins);
        }

        [Fact]
        public void Resolve_WhenScalarEnvOverrideAndJsonArrayChildren_PrefersScalar()
        {
            // Mimics appsettings.json array + Cors__AllowedOrigins env scalar merge.
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
                ["Cors:AllowedOrigins"] = "https://app.example.com"
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { "https://app.example.com" }, origins);
        }

        [Fact]
        public void Resolve_WhenCommaSeparatedString_SplitsOrigins()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://app.example.com,https://www.example.com"
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { "https://app.example.com", "https://www.example.com" }, origins);
        }

        [Fact]
        public void Resolve_WhenMixedCommaAndSemicolon_SplitsOrigins()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://app.example.com; https://www.example.com, https://admin.example.com"
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(
                new[] { "https://app.example.com", "https://www.example.com", "https://admin.example.com" },
                origins);
        }

        [Fact]
        public void Resolve_WhenOriginsHavePadding_TrimsWhitespace()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "  https://app.example.com  ,  https://www.example.com  "
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { "https://app.example.com", "https://www.example.com" }, origins);
        }

        [Fact]
        public void Resolve_WhenTrailingSlash_StripsSlash()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://app.example.com/"
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { "https://app.example.com" }, origins);
        }

        [Fact]
        public void Resolve_WhenJsonArrayTrailingSlash_StripsSlash()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.example.com/"
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { "https://app.example.com" }, origins);
        }

        [Fact]
        public void Resolve_WhenDuplicateOrigins_DeduplicatesIgnoringCase()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://App.Example.com",
                ["Cors:AllowedOrigins:1"] = "https://app.example.com/"
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Single(origins);
            Assert.Equal("https://App.Example.com", origins[0]);
        }

        [Fact]
        public void Resolve_WhenWildcard_Throws()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "*"
            });

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => CorsOriginsResolver.Resolve(configuration));

            Assert.Contains("must not contain '*'", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Resolve_WhenWildcardInJsonArray_Throws()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "*"
            });

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => CorsOriginsResolver.Resolve(configuration));

            Assert.Contains("must not contain '*'", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Resolve_WhenWildcardAmongOrigins_Throws()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://app.example.com;*;https://www.example.com"
            });

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => CorsOriginsResolver.Resolve(configuration));

            Assert.Contains("must not contain '*'", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Resolve_WhenEmptyEntries_IgnoresThem()
        {
            IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://app.example.com;;  ;"
            });

            string[] origins = CorsOriginsResolver.Resolve(configuration);

            Assert.Equal(new[] { "https://app.example.com" }, origins);
        }

        [Fact]
        public void Resolve_WhenNullConfiguration_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CorsOriginsResolver.Resolve(null!));
        }

        [Fact]
        public void PolicyName_IsSpa()
        {
            Assert.Equal("Spa", CorsOriginsResolver.PolicyName);
        }

        [Fact]
        public void DefaultOrigin_IsLocalAngularDevHost()
        {
            Assert.Equal("http://localhost:4200", CorsOriginsResolver.DefaultOrigin);
        }

        private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }
    }
}

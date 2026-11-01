using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Salzee.Services
{
    public class SiteConfig
    {
        public string? ProjectName { get; set; } = "PMS";
        public string? Tagline { get; set; } = "Property Management System";
        public string? WebsiteUrl { get; set; } = "";
        public string? Email { get; set; } = "";
        public string LogoPath { get; set; } = "~/images/logohome.png";
    }

    public interface ISiteConfigService
    {
        Task<SiteConfig> GetAsync();
        Task SaveAsync(SiteConfig config);
    }

    public class SiteConfigService : ISiteConfigService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _configPath;

        public SiteConfigService(IWebHostEnvironment env)
        {
            _env = env;
            var root = _env.ContentRootPath ?? AppContext.BaseDirectory;
            var configDir = Path.Combine(root, "App_Data");
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
            _configPath = Path.Combine(configDir, "siteconfig.json");
        }

        public async Task<SiteConfig> GetAsync()
        {
            if (!File.Exists(_configPath))
            {
                return new SiteConfig();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_configPath);
                var cfg = JsonSerializer.Deserialize<SiteConfig>(json);
                return cfg ?? new SiteConfig();
            }
            catch
            {
                return new SiteConfig();
            }
        }

        public async Task SaveAsync(SiteConfig config)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(_configPath, json);
        }
    }
}


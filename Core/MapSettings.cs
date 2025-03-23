using System.Text.Json;
using System.Text.Json.Serialization;

namespace MapChooserExtended.Core
{
    /// <summary>
    /// Class for managing map settings
    /// </summary>
    public class MapSettings
    {
        /// <summary>
        /// Path to the base settings file
        /// </summary>
        [JsonPropertyName("base")]
        public string? BasePath { get; set; }

        /// <summary>
        /// Map metadata
        /// </summary>
        [JsonPropertyName("meta")]
        public MapMeta Meta { get; set; } = new MapMeta();

        /// <summary>
        /// Map cycle settings
        /// </summary>
        [JsonPropertyName("settings")]
        public MapCycleSettings Settings { get; set; } = new MapCycleSettings();

        /// <summary>
        /// Load map settings from a file
        /// </summary>
        /// <param name="path">Path to the settings file</param>
        /// <returns>Whether the loading was successful</returns>
        public bool LoadFromFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var settings = JsonSerializer.Deserialize<MapSettings>(json, options);
                if (settings == null)
                    return false;

                BasePath = settings.BasePath;
                Meta = settings.Meta;
                Settings = settings.Settings;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCE] Error loading map settings from {path}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save map settings to a file
        /// </summary>
        /// <param name="path">Path to save the file</param>
        /// <returns>Whether the saving was successful</returns>
        public bool SaveToFile(string path)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(this, options);
                
                // Create directory if it doesn't exist
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCE] Error saving map settings to {path}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Merge with base settings
        /// </summary>
        /// <param name="baseSettings">Base settings</param>
        public void MergeWithBase(MapSettings baseSettings)
        {
            // Don't merge metadata (specific to each map)
            
            // If Settings is null, use base settings
            if (Settings == null)
            {
                Settings = baseSettings.Settings;
                return;
            }
            
            // Merge settings
            Settings.MergeWithBase(baseSettings.Settings);
        }

        /// <summary>
        /// Create default settings
        /// </summary>
        /// <returns>Default settings</returns>
        public static MapSettings CreateDefault()
        {
            return new MapSettings
            {
                Meta = new MapMeta
                {
                    Name = "default",
                    DisplayName = "",
                    WorkshopId = ""
                },
                Settings = MapCycleSettings.CreateDefault()
            };
        }
    }

    /// <summary>
    /// Map metadata
    /// </summary>
    public class MapMeta
    {
        /// <summary>
        /// Map name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Display name
        /// </summary>
        [JsonPropertyName("display")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Workshop ID
        /// </summary>
        [JsonPropertyName("workshop_id")]
        public string? WorkshopId { get; set; }
    }

    /// <summary>
    /// Map cycle settings
    /// </summary>
    public class MapCycleSettings
    {
        /// <summary>
        /// Whether map cycle is enabled
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Time periods (hours) to include in the map cycle
        /// </summary>
        [JsonPropertyName("times")]
        public int[] Times { get; set; } = []; // Default is all time periods

        /// <summary>
        /// Player count range
        /// </summary>
        [JsonPropertyName("players")]
        public PlayerRange Players { get; set; } = new PlayerRange();

        /// <summary>
        /// Cooldown settings
        /// </summary>
        [JsonPropertyName("cooldown")]
        public CooldownSettings Cooldown { get; set; } = new CooldownSettings();

        /// <summary>
        /// Nomination settings
        /// </summary>
        [JsonPropertyName("nomination")]
        public NominationSettings Nomination { get; set; } = new NominationSettings();

        /// <summary>
        /// Match settings
        /// </summary>
        [JsonPropertyName("match")]
        public MatchSettings Match { get; set; } = new MatchSettings();

        /// <summary>
        /// Extension settings
        /// </summary>
        [JsonPropertyName("extend")]
        public ExtendSettings Extend { get; set; } = new ExtendSettings();

        /// <summary>
        /// Additional cfg files to execute on map start
        /// </summary>
        [JsonPropertyName("cfgs")]
        public string[] Cfgs { get; set; } = [];

        /// <summary>
        /// Merge with base settings
        /// </summary>
        /// <param name="baseSettings">Base settings</param>
        public void MergeWithBase(MapCycleSettings baseSettings)
        {
            // Use base settings if not explicitly set
            // For arrays and objects, use base settings only if null or empty
            
            // Get default values for comparison
            var defaultSettings = CreateDefault();
            
            // Times
            if (Times == null || Times.Length == 0)
                Times = baseSettings.Times;
                
            // Enabled - use base setting if it's the default value
            if (Enabled == defaultSettings.Enabled)
                Enabled = baseSettings.Enabled;

            // Players
            Players.MergeWithBase(baseSettings.Players);

            // Cooldown
            Cooldown.MergeWithBase(baseSettings.Cooldown);

            // Nomination
            Nomination.MergeWithBase(baseSettings.Nomination);

            // Match
            Match.MergeWithBase(baseSettings.Match);

            // Extend
            Extend.MergeWithBase(baseSettings.Extend);

            // Cfgs
            if (Cfgs == null || Cfgs.Length == 0)
                Cfgs = baseSettings.Cfgs;
        }

        /// <summary>
        /// Create default settings
        /// </summary>
        /// <returns>Default settings</returns>
        public static MapCycleSettings CreateDefault()
        {
            return new MapCycleSettings
            {
                Enabled = true,
                Times = [],
                Players = new PlayerRange { Min = 0, Max = 64 },
                Cooldown = new CooldownSettings { Count = 0, Tags = [] },
                Nomination = new NominationSettings { Admin = false, Enabled = true },
                Match = new MatchSettings { Type = 0, Limit = "30" },
                Extend = new ExtendSettings { Enabled = true, Times = 2, Number = 15 },
                Cfgs = []
            };
        }
    }

    /// <summary>
    /// Player count range
    /// </summary>
    public class PlayerRange
    {
        /// <summary>
        /// Minimum player count
        /// </summary>
        [JsonPropertyName("min")]
        public int Min { get; set; } = 0;

        /// <summary>
        /// Maximum player count
        /// </summary>
        [JsonPropertyName("max")]
        public int Max { get; set; } = 64;

        /// <summary>
        /// Merge with base settings
        /// </summary>
        /// <param name="baseSettings">Base settings</param>
        public void MergeWithBase(PlayerRange baseSettings)
        {
            // Use base settings for default values
            // Compare with default values from MapCycleSettings.CreateDefault()
            var defaultPlayerRange = new PlayerRange { Min = 0, Max = 64 };
            
            if (Min == defaultPlayerRange.Min)
                Min = baseSettings.Min;
            
            if (Max == defaultPlayerRange.Max)
                Max = baseSettings.Max;
        }
    }

    /// <summary>
    /// Cooldown settings
    /// </summary>
    public class CooldownSettings
    {
        /// <summary>
        /// Cooldown count
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; } = 0;

        /// <summary>
        /// Current cooldown count
        /// </summary>
        [JsonPropertyName("current_count")]
        public int CurrentCount { get; set; } = 0;

        /// <summary>
        /// Cooldown tags
        /// </summary>
        [JsonPropertyName("tags")]
        public string[] Tags { get; set; } = [];

        /// <summary>
        /// Merge with base settings
        /// </summary>
        /// <param name="baseSettings">Base settings</param>
        public void MergeWithBase(CooldownSettings baseSettings)
        {
            // Use base settings for default values
            // Compare with default values from MapCycleSettings.CreateDefault()
            var defaultCooldownSettings = new CooldownSettings { Count = 0, Tags = [] };
            
            if (Count == defaultCooldownSettings.Count)
                Count = baseSettings.Count;
            
            if (Tags == null || Tags.Length == 0 || (Tags.Length == 1 && Tags[0] == "default"))
                Tags = baseSettings.Tags;
        }
    }

    /// <summary>
    /// Nomination settings
    /// </summary>
    public class NominationSettings
    {
        /// <summary>
        /// Whether only admins can nominate
        /// </summary>
        [JsonPropertyName("admin")]
        public bool Admin { get; set; } = false;

        /// <summary>
        /// Whether nomination is enabled
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Merge with base settings
        /// </summary>
        /// <param name="baseSettings">Base settings</param>
        public void MergeWithBase(NominationSettings baseSettings)
        {
            // Use base settings if not explicitly set
            // For boolean values, use base settings only for default values
            if (Admin == false)
                Admin = baseSettings.Admin;
            
            if (Enabled == true)
                Enabled = baseSettings.Enabled;
        }
    }

    /// <summary>
    /// Match settings
    /// </summary>
    public class MatchSettings
    {
        /// <summary>
        /// Match type (0: time limit, 1: round limit)
        /// </summary>
        [JsonPropertyName("type")]
        public int Type { get; set; } = 0;

        /// <summary>
        /// Limit value
        /// </summary>
        [JsonPropertyName("limit")]
        public string Limit { get; set; } = "30";

        /// <summary>
        /// Merge with base settings
        /// </summary>
        /// <param name="baseSettings">Base settings</param>
        public void MergeWithBase(MatchSettings baseSettings)
        {
            // Use base settings for default values
            // Compare with default values from MapCycleSettings.CreateDefault()
            var defaultMatchSettings = new MatchSettings { Type = 0, Limit = "30" };
            
            if (Type == defaultMatchSettings.Type)
                Type = baseSettings.Type;
            
            if (Limit == defaultMatchSettings.Limit)
                Limit = baseSettings.Limit;
        }
    }

    /// <summary>
    /// Extension settings
    /// </summary>
    public class ExtendSettings
    {
        /// <summary>
        /// Whether extension is enabled
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Number of possible extensions
        /// </summary>
        [JsonPropertyName("times")]
        public int Times { get; set; } = 2;

        /// <summary>
        /// Extension value
        /// </summary>
        [JsonPropertyName("number")]
        public int Number { get; set; } = 15;

        /// <summary>
        /// Merge with base settings
        /// </summary>
        /// <param name="baseSettings">Base settings</param>
        public void MergeWithBase(ExtendSettings baseSettings)
        {
            // Use base settings for default values
            // Compare with default values from MapCycleSettings.CreateDefault()
            var defaultExtendSettings = new ExtendSettings { Enabled = true, Times = 2, Number = 15 };
            
            if (Times == defaultExtendSettings.Times)
                Times = baseSettings.Times;
            
            if (Number == defaultExtendSettings.Number)
                Number = baseSettings.Number;
                
            if (Enabled == defaultExtendSettings.Enabled)
                Enabled = baseSettings.Enabled;
        }
    }
}
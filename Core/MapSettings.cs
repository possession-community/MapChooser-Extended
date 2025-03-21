using System.Text.Json;
using System.Text.Json.Serialization;

namespace cs2_rockthevote.Core
{
    /// <summary>
    /// マップの設定を管理するクラス
    /// </summary>
    public class MapSettings
    {
        /// <summary>
        /// ベース設定ファイルのパス
        /// </summary>
        [JsonPropertyName("base")]
        public string? BasePath { get; set; }

        /// <summary>
        /// マップのメタ情報
        /// </summary>
        [JsonPropertyName("meta")]
        public MapMeta Meta { get; set; } = new MapMeta();

        /// <summary>
        /// マップのサイクル設定
        /// </summary>
        [JsonPropertyName("settings")]
        public MapCycleSettings Settings { get; set; } = new MapCycleSettings();

        /// <summary>
        /// ファイルからマップ設定を読み込む
        /// </summary>
        /// <param name="path">設定ファイルのパス</param>
        /// <returns>読み込みに成功したかどうか</returns>
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
                Console.WriteLine($"[RockTheVote] Error loading map settings from {path}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// マップ設定をファイルに保存する
        /// </summary>
        /// <param name="path">保存先のパス</param>
        /// <returns>保存に成功したかどうか</returns>
        public bool SaveToFile(string path)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(this, options);
                
                // ディレクトリが存在しない場合は作成
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RockTheVote] Error saving map settings to {path}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ベース設定とマージする
        /// </summary>
        /// <param name="baseSettings">ベース設定</param>
        public void MergeWithBase(MapSettings baseSettings)
        {
            // メタ情報はマージしない（各マップ固有のもの）
            
            // 設定をマージ
            Settings.MergeWithBase(baseSettings.Settings);
        }

        /// <summary>
        /// デフォルト設定を作成する
        /// </summary>
        /// <returns>デフォルト設定</returns>
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
    /// マップのメタ情報
    /// </summary>
    public class MapMeta
    {
        /// <summary>
        /// マップ名
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 表示名
        /// </summary>
        [JsonPropertyName("display")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// ワークショップID
        /// </summary>
        [JsonPropertyName("workshop_id")]
        public string? WorkshopId { get; set; }
    }

    /// <summary>
    /// マップのサイクル設定
    /// </summary>
    public class MapCycleSettings
    {
        /// <summary>
        /// マップサイクルが有効かどうか
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// マップサイクルに含める時間帯（時）
        /// </summary>
        [JsonPropertyName("times")]
        public int[] Times { get; set; } = []; // デフォルトは全時間帯

        /// <summary>
        /// プレイヤー数の範囲
        /// </summary>
        [JsonPropertyName("players")]
        public PlayerRange Players { get; set; } = new PlayerRange();

        /// <summary>
        /// クールダウン設定
        /// </summary>
        [JsonPropertyName("cooldown")]
        public CooldownSettings Cooldown { get; set; } = new CooldownSettings();

        /// <summary>
        /// ノミネーション設定
        /// </summary>
        [JsonPropertyName("nomination")]
        public NominationSettings Nomination { get; set; } = new NominationSettings();

        /// <summary>
        /// マッチ設定
        /// </summary>
        [JsonPropertyName("match")]
        public MatchSettings Match { get; set; } = new MatchSettings();

        /// <summary>
        /// 延長設定
        /// </summary>
        [JsonPropertyName("extend")]
        public ExtendSettings Extend { get; set; } = new ExtendSettings();

        /// <summary>
        /// ベース設定とマージする
        /// </summary>
        /// <param name="baseSettings">ベース設定</param>
        public void MergeWithBase(MapCycleSettings baseSettings)
        {
            // 明示的に設定されていない場合はベース設定を使用
            // 配列やオブジェクトはnullの場合のみベース設定を使用
            
            // Times
            if (Times == null || Times.Length == 0)
                Times = baseSettings.Times;

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
        }

        /// <summary>
        /// デフォルト設定を作成する
        /// </summary>
        /// <returns>デフォルト設定</returns>
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
                Extend = new ExtendSettings { Enabled = true, Times = 2, Number = 15 }
            };
        }
    }

    /// <summary>
    /// プレイヤー数の範囲
    /// </summary>
    public class PlayerRange
    {
        /// <summary>
        /// 最小プレイヤー数
        /// </summary>
        [JsonPropertyName("min")]
        public int Min { get; set; } = 0;

        /// <summary>
        /// 最大プレイヤー数
        /// </summary>
        [JsonPropertyName("max")]
        public int Max { get; set; } = 64;

        /// <summary>
        /// ベース設定とマージする
        /// </summary>
        /// <param name="baseSettings">ベース設定</param>
        public void MergeWithBase(PlayerRange baseSettings)
        {
            // デフォルト値の場合はベース設定を使用
            if (Min == 0)
                Min = baseSettings.Min;
            
            if (Max == 64)
                Max = baseSettings.Max;
        }
    }

    /// <summary>
    /// クールダウン設定
    /// </summary>
    public class CooldownSettings
    {
        /// <summary>
        /// クールダウン回数
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; } = 0;

        /// <summary>
        /// 現在のクールダウン回数
        /// </summary>
        [JsonPropertyName("current_count")]
        public int CurrentCount { get; set; } = 0;

        /// <summary>
        /// クールダウンタグ
        /// </summary>
        [JsonPropertyName("tags")]
        public string[] Tags { get; set; } = [];

        /// <summary>
        /// ベース設定とマージする
        /// </summary>
        /// <param name="baseSettings">ベース設定</param>
        public void MergeWithBase(CooldownSettings baseSettings)
        {
            // デフォルト値の場合はベース設定を使用
            if (Count == 2)
                Count = baseSettings.Count;
            
            if (Tags == null || Tags.Length == 0 || (Tags.Length == 1 && Tags[0] == "default"))
                Tags = baseSettings.Tags;
        }
    }

    /// <summary>
    /// ノミネーション設定
    /// </summary>
    public class NominationSettings
    {
        /// <summary>
        /// 管理者のみノミネーション可能かどうか
        /// </summary>
        [JsonPropertyName("admin")]
        public bool Admin { get; set; } = false;

        /// <summary>
        /// ノミネーションが有効かどうか
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// ベース設定とマージする
        /// </summary>
        /// <param name="baseSettings">ベース設定</param>
        public void MergeWithBase(NominationSettings baseSettings)
        {
            // 明示的に設定されていない場合はベース設定を使用
            // boolはデフォルト値の場合のみベース設定を使用
        }
    }

    /// <summary>
    /// マッチ設定
    /// </summary>
    public class MatchSettings
    {
        /// <summary>
        /// マッチタイプ（0: 時間制限, 1: ラウンド制限）
        /// </summary>
        [JsonPropertyName("type")]
        public int Type { get; set; } = 0;

        /// <summary>
        /// 制限値
        /// </summary>
        [JsonPropertyName("limit")]
        public string Limit { get; set; } = "30";

        /// <summary>
        /// ベース設定とマージする
        /// </summary>
        /// <param name="baseSettings">ベース設定</param>
        public void MergeWithBase(MatchSettings baseSettings)
        {
            // デフォルト値の場合はベース設定を使用
            if (Type == 0)
                Type = baseSettings.Type;
            
            if (Limit == "30")
                Limit = baseSettings.Limit;
        }
    }

    /// <summary>
    /// 延長設定
    /// </summary>
    public class ExtendSettings
    {
        /// <summary>
        /// 延長が有効かどうか
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 延長可能回数
        /// </summary>
        [JsonPropertyName("times")]
        public int Times { get; set; } = 2;

        /// <summary>
        /// 延長値
        /// </summary>
        [JsonPropertyName("number")]
        public int Number { get; set; } = 15;

        /// <summary>
        /// ベース設定とマージする
        /// </summary>
        /// <param name="baseSettings">ベース設定</param>
        public void MergeWithBase(ExtendSettings baseSettings)
        {
            // デフォルト値の場合はベース設定を使用
            if (Times == 2)
                Times = baseSettings.Times;
            
            if (Number == 15)
                Number = baseSettings.Number;
        }
    }
}
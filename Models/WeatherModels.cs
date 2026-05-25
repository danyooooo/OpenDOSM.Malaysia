using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenDOSM.Malaysia.Models
{
    public class WeatherForecast
    {
        [JsonPropertyName("location")]
        public LocationInfo Location { get; set; } = new();

        [JsonPropertyName("date")]
        public string Date { get; set; } = "";

        [JsonIgnore]
        public string DayOfWeek
        {
            get
            {
                if (DateTime.TryParse(Date, out var dt))
                    return dt.ToString("dddd");
                return Date;
            }
        }

        [JsonPropertyName("morning_forecast")]
        public string MorningForecast { get; set; } = "";

        [JsonPropertyName("afternoon_forecast")]
        public string AfternoonForecast { get; set; } = "";

        [JsonPropertyName("night_forecast")]
        public string NightForecast { get; set; } = "";

        [JsonPropertyName("summary_forecast")]
        public string SummaryForecast { get; set; } = "";

        [JsonPropertyName("summary_when")]
        public string SummaryWhen { get; set; } = "";

        [JsonPropertyName("min_temp")]
        public int MinTemp { get; set; }

        [JsonPropertyName("max_temp")]
        public int MaxTemp { get; set; }
    }

    public class LocationInfo
    {
        [JsonPropertyName("location_id")]
        public string LocationId { get; set; } = "";

        [JsonPropertyName("location_name")]
        public string LocationName { get; set; } = "";
    }

    public class WeatherWarning
    {
        [JsonPropertyName("warning_issue")]
        public WarningIssue Issue { get; set; } = new();

        [JsonPropertyName("valid_from")]
        public DateTime? ValidFrom { get; set; }

        [JsonPropertyName("valid_to")]
        public DateTime? ValidTo { get; set; }

        [JsonPropertyName("heading_en")]
        public string HeadingEn { get; set; } = "";

        [JsonPropertyName("text_en")]
        public string TextEn { get; set; } = "";

        [JsonPropertyName("instruction_en")]
        public string? InstructionEn { get; set; }

        [JsonPropertyName("heading_bm")]
        public string HeadingBm { get; set; } = "";

        [JsonPropertyName("text_bm")]
        public string TextBm { get; set; } = "";

        [JsonPropertyName("instruction_bm")]
        public string? InstructionBm { get; set; }
    }

    public class WarningIssue
    {
        [JsonPropertyName("issued")]
        public DateTime Issued { get; set; }

        [JsonPropertyName("title_bm")]
        public string TitleBm { get; set; } = "";

        [JsonPropertyName("title_en")]
        public string TitleEn { get; set; } = "";
    }
}

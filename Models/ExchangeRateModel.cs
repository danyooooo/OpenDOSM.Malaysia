using System.Text.Json.Serialization;

namespace OpenDOSM.Malaysia.Models;

public class ExchangeRateModel
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("rate_type")]
    public string? RateType { get; set; } // buying, middle, selling

    [JsonPropertyName("aed")] public double? Aed { get; set; }
    [JsonPropertyName("aud")] public double? Aud { get; set; }
    [JsonPropertyName("bnd")] public double? Bnd { get; set; }
    [JsonPropertyName("cad")] public double? Cad { get; set; }
    [JsonPropertyName("chf")] public double? Chf { get; set; }
    [JsonPropertyName("cny")] public double? Cny { get; set; }
    [JsonPropertyName("egp")] public double? Egp { get; set; }
    [JsonPropertyName("eur")] public double? Eur { get; set; }
    [JsonPropertyName("gbp")] public double? Gbp { get; set; }
    [JsonPropertyName("hkd")] public double? Hkd { get; set; }
    [JsonPropertyName("idr")] public double? Idr { get; set; }
    [JsonPropertyName("inr")] public double? Inr { get; set; }
    [JsonPropertyName("jpy")] public double? Jpy { get; set; }
    [JsonPropertyName("khr")] public double? Khr { get; set; }
    [JsonPropertyName("krw")] public double? Krw { get; set; }
    [JsonPropertyName("mmk")] public double? Mmk { get; set; }
    [JsonPropertyName("npr")] public double? Npr { get; set; }
    [JsonPropertyName("nzd")] public double? Nzd { get; set; }
    [JsonPropertyName("php")] public double? Php { get; set; }
    [JsonPropertyName("pkr")] public double? Pkr { get; set; }
    [JsonPropertyName("sar")] public double? Sar { get; set; }
    [JsonPropertyName("sgd")] public double? Sgd { get; set; }
    [JsonPropertyName("thb")] public double? Thb { get; set; }
    [JsonPropertyName("twd")] public double? Twd { get; set; }
    [JsonPropertyName("usd")] public double? Usd { get; set; }
    [JsonPropertyName("vnd")] public double? Vnd { get; set; }
    [JsonPropertyName("xdr")] public double? Xdr { get; set; }
}

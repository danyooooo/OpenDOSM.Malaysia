using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenDOSM.Malaysia.Services;

public class DataCatalogueService : IDataCatalogueService
{
    private readonly HttpClient _httpClient;

    internal DataCatalogueService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<T>> GetDatasetAsync<T>(string id, int limit = -1)
    {
        try
        {
            var url = $"https://api.data.gov.my/data-catalogue?id={id}";
            if (limit > 0)
            {
                url += $"&limit={limit}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<List<T>>(json, options);
            
            return result ?? new List<T>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching data catalogue dataset '{id}': {ex.Message}");
            return new List<T>();
        }
    }
}

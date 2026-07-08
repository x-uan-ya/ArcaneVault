// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Analytics
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public IndexModel(IHttpClientFactory http) => _http = http;

        public SummaryDto?           Summary           { get; set; }
        public List<CategoryStat>    ItemsPerCategory  { get; set; } = new();
        public List<CollectorStat>   TopCollectors     { get; set; } = new();
        public List<GrowthStat>      CollectionGrowth  { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Only Staff can view analytics
            if (!SessionHelper.IsStaff(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            var client = _http.CreateClient("API");
            
            // Add JWT token to all API requests
            client.SetAuthorizationToken(HttpContext.Session);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Fetch all four endpoints in parallel for speed
            var t1 = client.GetAsync("api/analytics/summary");
            var t2 = client.GetAsync("api/analytics/items-per-category");
            var t3 = client.GetAsync("api/analytics/top-collectors");
            var t4 = client.GetAsync("api/analytics/collection-growth");

            await Task.WhenAll(t1, t2, t3, t4);

            if (t1.Result.IsSuccessStatusCode)
                Summary = JsonSerializer.Deserialize<SummaryDto>(
                    await t1.Result.Content.ReadAsStringAsync(), options);

            if (t2.Result.IsSuccessStatusCode)
                ItemsPerCategory = JsonSerializer.Deserialize<List<CategoryStat>>(
                    await t2.Result.Content.ReadAsStringAsync(), options) ?? new();

            if (t3.Result.IsSuccessStatusCode)
                TopCollectors = JsonSerializer.Deserialize<List<CollectorStat>>(
                    await t3.Result.Content.ReadAsStringAsync(), options) ?? new();

            if (t4.Result.IsSuccessStatusCode)
                CollectionGrowth = JsonSerializer.Deserialize<List<GrowthStat>>(
                    await t4.Result.Content.ReadAsStringAsync(), options) ?? new();

            return Page();
        }

        // DTOs matching the API response shapes
        public class SummaryDto
        {
            public int TotalUsers      { get; set; }
            public int TotalItems      { get; set; }
            public int TotalCategories { get; set; }
        }

        public class CategoryStat
        {
            public string CategoryCode { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public int    ItemCount    { get; set; }
        }

        public class CollectorStat
        {
            public string UserName       { get; set; } = string.Empty;
            public int    TotalItems     { get; set; }
            public int    TotalCurrentQty { get; set; }
        }

        public class GrowthStat
        {
            public string CategoryName  { get; set; } = string.Empty;
            public int    TotalStarting { get; set; }
            public int    TotalCurrent  { get; set; }
        }

        /// <summary>
        /// Handles CSV download request with JWT authentication.
        /// </summary>
        public async Task<IActionResult> OnGetDownloadCsvAsync()
        {
            // Verify staff access
            if (!SessionHelper.IsStaff(HttpContext.Session))
                return Unauthorized();

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.GetAsync("api/analytics/export-csv");
                
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    return File(fileBytes, "text/csv", "analytics-export.csv");
                }

                return BadRequest("Failed to export CSV from API.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error exporting CSV: {ex.Message}");
            }
        }
    }
}

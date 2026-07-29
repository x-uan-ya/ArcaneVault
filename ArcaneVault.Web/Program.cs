// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;

namespace ArcaneVault.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorPages();

            // Session
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Named HttpClient pointing at the API
            builder.Services.AddHttpClient("API", client =>
            {
                string apiBase = builder.Configuration["ApiBaseUrl"]
                    ?? "https://localhost:7129";
                client.BaseAddress = new Uri(apiBase);
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();

            // ── Notification proxy endpoints ─────────────────────────────────
            // These let the browser call /api-proxy/notifications without
            // knowing the API base URL. The JWT from session is attached here.

            app.MapGet("/api-proxy/notifications", async (HttpContext ctx, IHttpClientFactory http) =>
            {
                var token = ctx.Session.GetString(SessionHelper.KeyJwtToken);
                if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

                var client = http.CreateClient("API");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var res = await client.GetAsync("api/notifications");
                var body = await res.Content.ReadAsStringAsync();
                return Results.Content(body, "application/json", statusCode: (int)res.StatusCode);
            });

            app.MapPut("/api-proxy/notifications/{id}/read", async (int id, HttpContext ctx, IHttpClientFactory http) =>
            {
                var token = ctx.Session.GetString(SessionHelper.KeyJwtToken);
                if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

                var client = http.CreateClient("API");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var res = await client.PutAsync($"api/notifications/{id}/read", null);
                return Results.StatusCode((int)res.StatusCode);
            });

            app.MapPut("/api-proxy/notifications/read-all", async (HttpContext ctx, IHttpClientFactory http) =>
            {
                var token = ctx.Session.GetString(SessionHelper.KeyJwtToken);
                if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

                var client = http.CreateClient("API");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var res = await client.PutAsync("api/notifications/read-all", null);
                return Results.StatusCode((int)res.StatusCode);
            });

            app.MapDelete("/api-proxy/notifications", async (HttpContext ctx, IHttpClientFactory http) =>
            {
                var token = ctx.Session.GetString(SessionHelper.KeyJwtToken);
                if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

                var client = http.CreateClient("API");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var res = await client.DeleteAsync("api/notifications");
                return Results.StatusCode((int)res.StatusCode);
            });
            // ─────────────────────────────────────────────────────────────────

            app.MapStaticAssets();
            app.MapRazorPages().WithStaticAssets();

            app.Run();
        }
    }
}

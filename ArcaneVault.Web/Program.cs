// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

namespace ArcaneVault.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // The rest of your standard configuration remains the same:
            // Razor Pages
            builder.Services.AddRazorPages();

            // Session (used for login state)
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

            // Session must be before Authorization
            app.UseSession();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages().WithStaticAssets();

            app.Run();
        }
    }
}

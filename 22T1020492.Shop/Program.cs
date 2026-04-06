using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// --- BỔ SUNG: CẤU HÌNH DỊCH VỤ SESSION ---
builder.Services.AddDistributedMemoryCache(); // Cần thiết để lưu trữ Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session hết hạn sau 30 phút không hoạt động
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 1. LẤY CHUỖI KẾT NỐI (Phải đúng tên LiteCommerceDB như Admin)
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("Không tìm thấy chuỗi kết nối 'LiteCommerceDB' trong appsettings.json.");

// 2. KHỞI TẠO BUSINESS LAYER
SV22T1020492.BusinessLayers.Configuration.Initialize(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

// --- BỔ SUNG: KÍCH HOẠT SESSION ---
// Lưu ý: app.UseSession() phải nằm SAU UseRouting() và TRƯỚC MapControllerRoute
app.UseRouting();
app.UseSession();

// Cấu hình ngôn ngữ Tiếng Việt
var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

app.UseAuthorization(); // Nên thêm dòng này nếu có đăng nhập

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
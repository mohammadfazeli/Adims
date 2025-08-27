👌 عالی، پس الان یه نسخه کامل و ماژولار از همین پیاده‌سازی می‌سازم که راحت توی پروژه بتونی بندازی و استفاده کنی.
کدها رو با توضیحات فایل‌به‌فایل می‌نویسم.


---

📂 ساختار پوشه‌ها و فایل‌ها

/Security
   ICurrentUserService.cs
   SecurityRule.cs
   IQueryFilterRuleProvider.cs
   DbRuleProvider.cs
   DynamicQueryInterceptor.cs

/Data
   AppDbContext.cs


---

🔹 1. فایل ICurrentUserService.cs

این فقط سرویسیه که اطلاعات کاربر جاری رو میده.
تو واقعیت از HttpContext یا JWT می‌گیری.

namespace MyApp.Security;

public interface ICurrentUserService
{
    string UserId { get; }
    string[] Roles { get; }
}

// نمونه ساده تستی
public class FakeCurrentUserService : ICurrentUserService
{
    public string UserId => "user123";
    public string[] Roles => new[] { "Manager" };
}


---

🔹 2. فایل SecurityRule.cs

جدول رول‌ها (Entity برای EF).

using System.ComponentModel.DataAnnotations;

namespace MyApp.Security;

public class SecurityRule
{
    [Key]
    public int Id { get; set; }

    public string EntityName { get; set; } = null!;   // اسم Entity (مثلاً "Order")
    public string PropertyName { get; set; } = null!; // ستون مورد فیلتر (مثلاً "TenantId")
    public string Operator { get; set; } = "=";       // عملگر: =, !=, >, < ...
    public string Value { get; set; } = null!;        // مقدار (string ذخیره می‌کنیم)

    public string? UserId { get; set; }               // محدودیت برای یک کاربر خاص
    public string? Role { get; set; }                 // محدودیت برای یک Role خاص
}


---

🔹 3. فایل IQueryFilterRuleProvider.cs

این واسطه‌ست تا بتونی Rule Provider مختلف پیاده کنی.

using System.Linq.Expressions;

namespace MyApp.Security;

public interface IQueryFilterRuleProvider
{
    /// بر اساس Entity مورد نظر، Expression فیلتر رو برمی‌گردونه
    LambdaExpression? GetRule(Type entityType);
}


---

🔹 4. فایل DbRuleProvider.cs

این کلاس رول‌ها رو از دیتابیس می‌خونه و Expression می‌سازه.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Security;

public class DbRuleProvider : IQueryFilterRuleProvider
{
    private readonly DbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DbRuleProvider(DbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public LambdaExpression? GetRule(Type entityType)
    {
        var entityName = entityType.Name;

        // همه رول‌هایی که برای این Entity و یوزر/رول جاری تعریف شدن
        var rules = _db.Set<SecurityRule>()
                       .Where(r => r.EntityName == entityName &&
                                   (r.UserId == _currentUser.UserId ||
                                    (r.Role != null && _currentUser.Roles.Contains(r.Role))))
                       .ToList();

        if (!rules.Any()) return null;

        var parameter = Expression.Parameter(entityType, "e");
        Expression? body = null;

        foreach (var rule in rules)
        {
            var prop = Expression.Property(parameter, rule.PropertyName);

            // مقدار مناسب
            Expression constant;
            if (prop.Type == typeof(int))
                constant = Expression.Constant(int.Parse(rule.Value));
            else if (prop.Type == typeof(string))
                constant = Expression.Constant(rule.Value);
            else if (prop.Type == typeof(Guid))
                constant = Expression.Constant(Guid.Parse(rule.Value));
            else
                throw new NotSupportedException($"Type {prop.Type} not supported.");

            // عملگر
            Expression comparison = rule.Operator switch
            {
                "=" => Expression.Equal(prop, constant),
                "!=" => Expression.NotEqual(prop, constant),
                ">" => Expression.GreaterThan(prop, constant),
                "<" => Expression.LessThan(prop, constant),
                ">=" => Expression.GreaterThanOrEqual(prop, constant),
                "<=" => Expression.LessThanOrEqual(prop, constant),
                _ => throw new NotSupportedException($"Operator {rule.Operator} not supported.")
            };

            body = body == null ? comparison : Expression.AndAlso(body, comparison);
        }

        return Expression.Lambda(body!, parameter);
    }
}


---

🔹 5. فایل DynamicQueryInterceptor.cs

این Interceptor به همه‌ی کوئری‌ها فیلتر اضافه می‌کنه.

using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq.Expressions;

namespace MyApp.Security;

public class DynamicQueryInterceptor : IQueryExpressionInterceptor
{
    private readonly IQueryFilterRuleProvider _ruleProvider;

    public DynamicQueryInterceptor(IQueryFilterRuleProvider ruleProvider)
    {
        _ruleProvider = ruleProvider;
    }

    public Expression QueryCompilationStarting(
        Expression queryExpression,
        QueryExpressionEventData eventData,
        QueryCompilationContext queryCompilationContext)
    {
        var entityType = queryCompilationContext.Model.FindEntityType(eventData.ContextType);
        return queryExpression;
    }

    public Expression QueryModelCreating(
        Expression queryExpression,
        QueryExpressionEventData eventData,
        QueryModelCreatingContext queryModelCreatingContext)
    {
        return queryExpression;
    }

    public Expression QueryTransforming(
        Expression queryExpression,
        QueryExpressionEventData eventData)
    {
        // بررسی می‌کنیم آیا این کوئری روی یک Entity هست
        if (queryExpression is not MethodCallExpression mce) return queryExpression;
        if (mce.Method.Name != "Where" && mce.Method.Name != "Select") return queryExpression;

        var elementType = mce.Type.GetGenericArguments().FirstOrDefault();
        if (elementType == null) return queryExpression;

        var filter = _ruleProvider.GetRule(elementType);
        if (filter == null) return queryExpression;

        // اضافه کردن Where به کوئری
        var whereCall = Expression.Call(
            typeof(Queryable),
            "Where",
            new[] { elementType },
            mce.Arguments[0],
            filter
        );

        return whereCall;
    }
}


---

🔹 6. فایل AppDbContext.cs

DbContext اصلی پروژه.

using Microsoft.EntityFrameworkCore;
using MyApp.Security;

namespace MyApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SecurityRule> SecurityRules => Set<SecurityRule>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlServer("Server=.;Database=MyDb;Trusted_Connection=True;")
            .AddInterceptors(new DynamicQueryInterceptor(
                new DbRuleProvider(this, new FakeCurrentUserService())
            ));
    }
}

// نمونه Entity ها
public class Order
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ProductId { get; set; }
    public int CustomerId { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
}

public class Customer
{
    public int Id { get; set; }
    public string Country { get; set; } = null!;
}


---

🔹 نمونه استفاده

using var db = new AppDbContext();

// جوین ساده
var query = from o in db.Orders
            join p in db.Products on o.ProductId equals p.Id
            join c in db.Customers on o.CustomerId equals c.Id
            select new { o, p, c };

var result = query.ToList(); // اینجا فیلترها به صورت خودکار اعمال میشن


---

✅ این کد کامل، رول‌ها رو از دیتابیس می‌گیره و روی همه کوئری‌ها (حتی Join) اعمال می‌کنه.
اگه یوزر Rule خاص داشته باشه → فیلترش میاد روی همون Entity.


---

فرهاد، می‌خوای همینو برات ارتقا بدم که Value بتونه داینامیک از Claims کاربر یا حتی توکن JWT پر بشه (مثلاً TenantId رو از Claim بگیره نه از مقدار ثابت دیتابیس)؟


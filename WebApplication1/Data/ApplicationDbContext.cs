using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;
using UUIDNext;

namespace WebApplication1.Data;

public class User : IdentityUser<Guid>
{
    public string FullName { get; set; }

    public virtual UserDetail UserDetail { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class Role : IdentityRole<Guid>
{
    public string Description { get; set; }
    public bool IsDefault { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}


public class UserRole : IdentityUserRole<Guid>
{
    public User User { get; set; }
    public Role Role { get; set; }
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Role Role { get; set; }
    public Permission Permission { get; set; }
}

public class UserDetail
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string IdentityNumber { get; set; }
    public DateTime? Birthdate { get; set; }
    public int? UserTypeId { get; set; }
    public int? GenderId { get; set; }
    public int? NationalityId { get; set; }
    public string Address { get; set; }
    public string Extra { get; private set; }  // To store JSON data as string

    public virtual User User { get; set; }


    public T? GetExtra<T>()
    {
        if (string.IsNullOrEmpty(Extra))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(Extra);
        }
        catch (Exception)
        {
            return default;
        }
    }

    public void SetExtra<T>(T o)
    {
        Extra = JsonSerializer.Serialize(o);
    }
}




public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, Role, Guid, IdentityUserClaim<Guid>, UserRole,
        IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options)
{
    public DbSet<UserDetail> UserDetails { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Set default schema
        builder.HasDefaultSchema("auth");

        builder.Entity<User>(b =>
        {
            b.Property(e => e.Id).HasValueGenerator<UuidSequentialValueGenerator>();
            b.Property(e => e.FullName).HasMaxLength(256).IsRequired();
        });


        builder.Entity<Role>(b =>
        {
            b.Property(e => e.Id).HasValueGenerator<UuidSequentialValueGenerator>();
            b.Property(e => e.Id).HasColumnOrder(1);
            b.Property(e => e.Name).HasColumnOrder(2);
            b.Property(e => e.Description).HasColumnOrder(3).HasMaxLength(500).IsRequired(false);
        });


        builder.Entity<UserDetail>(b =>
        {
            b.ToTable($"AspNet{nameof(UserDetail)}s");

            b.Property(e => e.Id).HasValueGenerator<UuidSequentialValueGenerator>();
            b.Property(e => e.IdentityNumber).HasMaxLength(14).IsRequired(false);
            b.Property(e => e.Address).HasMaxLength(500).IsRequired(false);
            b.Property(e => e.Extra).IsRequired(false);

            b.HasOne(ud => ud.User)
                .WithOne(u => u.UserDetail)
                .HasForeignKey<UserDetail>(ud => ud.UserId);
        });


        builder.Entity<Permission>(b =>
        {
            b.ToTable($"AspNet{nameof(Permission)}s");

            b.Property(e => e.Id).HasValueGenerator<UuidSequentialValueGenerator>();
            b.Property(e => e.Name).HasMaxLength(256).IsRequired();
            b.Property(e => e.Description).HasMaxLength(500).IsRequired(false);
        });


        builder.Entity<UserRole>(b =>
        {
            b.HasKey(ur => new { ur.UserId, ur.RoleId });

            b.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            b.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);
        });


        builder.Entity<RolePermission>(b =>
        {
            b.ToTable($"AspNet{nameof(RolePermission)}s");

            b.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            b.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            b.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);
        });
    }
}


public class UuidSequentialValueGenerator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry)
        => Uuid.NewSequential();
}

public class AppClaims
{
    public const string Id = "Id";
    public const string FullName = "FullName";
    public const string UserName = "UserName";
    public const string Email = "Email";
    public const string Roles = "Roles";
    public const string IsAdministrator = "IsAdministrator";
}

public class AdditionalUserClaimsPrincipalFactory(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<User, Role>(userManager, roleManager, optionsAccessor)
{
    public override async Task<ClaimsPrincipal> CreateAsync(User user)
    {
        var principal = await base.CreateAsync(user);
        if (principal.Identity is not ClaimsIdentity identity) return principal;

        identity.AddClaim(new Claim(AppClaims.Id, user.Id.ToString()));
        identity.AddClaim(new Claim(AppClaims.FullName, user.FullName));

        return principal;
    }
}
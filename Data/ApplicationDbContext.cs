using Foras_Khadra.Models;
using Foras_Khadra.ViewModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace Foras_Khadra.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Country> Countries { get; set; }

        public DbSet<ManualOrganization> ManualOrganizations { get; set; }
        public DbSet<TeamMember> TeamMember { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<Opportunity> Opportunities { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<ReelsRequest> ReelsRequests { get; set; }
        public DbSet<SavedOpportunity> SavedOpportunities { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SavedSearch> SavedSearches { get; set; }
        public DbSet<FlagshipProgram> FlagshipPrograms { get; set; }
        public DbSet<ImpactStat> ImpactStats { get; set; }

        // نص البحث المطبّع للفرصة يُحدَّث مع كل إضافة أو تعديل أيًّا كان مصدره (الإدارة، المنظمة، منصة الفريق، البذر)
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            RefreshSearchText();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            RefreshSearchText();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void RefreshSearchText()
        {
            foreach (var entry in ChangeTracker.Entries<Opportunity>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                    entry.Entity.SearchText = Helpers.SearchIndexer.Build(entry.Entity);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // المشاريع الرائدة: الـ slug فريد، وحذف المشروع يفكّ ربط نشاطاته ولا يحذفها
            modelBuilder.Entity<FlagshipProgram>().HasIndex(p => p.Slug).IsUnique();
            modelBuilder.Entity<Activity>()
                .HasOne(a => a.Program)
                .WithMany()
                .HasForeignKey(a => a.ProgramId)
                .OnDelete(DeleteBehavior.SetNull);

            // فهارس مسار منصة الفريق (أُنشئت في هجرة TeamPipeline اليدوية؛ تُعلن هنا حتى لا تحذفها الهجرات التالية)
            modelBuilder.Entity<Opportunity>().HasIndex(o => o.PipeId);
            modelBuilder.Entity<Opportunity>().HasIndex(o => o.Status);

            // الفرص المحفوظة: لا تكرار لنفس الفرصة عند نفس المستخدم، وتُحذف مع حذف الفرصة.
            // جهة المستخدم بلا cascade في القاعدة (SQL Server يرفض مسارين: المستخدم → المحفوظات والمستخدم → فرصه → المحفوظات)؛
            // لا يوجد حذف مستخدمين في الموقع حاليًا؛ إن أُضيف، احذف SavedOpportunities الخاصة به أولًا (ClientCascade يفعلها فقط للصفوف المحمّلة).
            modelBuilder.Entity<SavedOpportunity>()
                .HasIndex(s => new { s.UserId, s.OpportunityId })
                .IsUnique();
            modelBuilder.Entity<SavedOpportunity>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.ClientCascade);
            modelBuilder.Entity<SavedOpportunity>()
                .HasOne(s => s.Opportunity)
                .WithMany()
                .HasForeignKey(s => s.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            // الإشعارات والبحوث المحفوظة: جهة المستخدم بلا cascade في القاعدة لنفس سبب SavedOpportunities أعلاه.
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.ReadAt });
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.ClientCascade);
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Opportunity)
                .WithMany()
                .HasForeignKey(n => n.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavedSearch>()
                .HasIndex(s => s.UserId);
            modelBuilder.Entity<SavedSearch>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<Country>().HasData(

    // Asia
    new Country { Id = 1, NameAr = "كل دول العالم", NameEn = "All World", NameFr = "Tous les pays du monde" },
    new Country { Id = 2, NameAr = "الوطن العربي ", NameEn = "The Arab World", NameFr = "Le Monde arabe" },
    new Country { Id = 3, NameAr = "فلسطين", NameEn = "Palestine", NameFr = "Palestine" },
    new Country { Id = 4, NameAr = "الأردن", NameEn = "Jordan", NameFr = "Jordanie" },
    new Country { Id = 5, NameAr = "سوريا", NameEn = "Syria", NameFr = "Syrie" },
    new Country { Id = 6, NameAr = "لبنان", NameEn = "Lebanon", NameFr = "Liban" },
    new Country { Id = 7, NameAr = "العراق", NameEn = "Iraq", NameFr = "Irak" },
    new Country { Id = 8, NameAr = "السعودية", NameEn = "Saudi Arabia", NameFr = "Arabie saoudite" },
    new Country { Id = 9, NameAr = "الكويت", NameEn = "Kuwait", NameFr = "Koweït" },
    new Country { Id = 10, NameAr = "قطر", NameEn = "Qatar", NameFr = "Qatar" },
    new Country { Id = 11, NameAr = "البحرين", NameEn = "Bahrain", NameFr = "Bahreïn" },
    new Country { Id = 12, NameAr = "الإمارات العربية المتحدة", NameEn = "United Arab Emirates", NameFr = "Émirats arabes unis" },
    new Country { Id = 13, NameAr = "عُمان", NameEn = "Oman", NameFr = "Oman" },
    new Country { Id = 14, NameAr = "اليمن", NameEn = "Yemen", NameFr = "Yémen" },

    // Africa
    new Country { Id = 15, NameAr = "مصر", NameEn = "Egypt", NameFr = "Égypte" },
    new Country { Id = 16, NameAr = "السودان", NameEn = "Sudan", NameFr = "Soudan" },
    new Country { Id = 17, NameAr = "ليبيا", NameEn = "Libya", NameFr = "Libye" },
    new Country { Id = 18, NameAr = "تونس", NameEn = "Tunisia", NameFr = "Tunisie" },
    new Country { Id = 19, NameAr = "الجزائر", NameEn = "Algeria", NameFr = "Algérie" },
    new Country { Id = 20, NameAr = "المغرب", NameEn = "Morocco", NameFr = "Maroc" },
    new Country { Id = 21, NameAr = "موريتانيا", NameEn = "Mauritania", NameFr = "Mauritanie" },
    new Country { Id = 22, NameAr = "جيبوتي", NameEn = "Djibouti", NameFr = "Djibouti" },
    new Country { Id = 23, NameAr = "الصومال", NameEn = "Somalia", NameFr = "Somalie" },
    new Country { Id = 24, NameAr = "جزر القمر", NameEn = "Comoros", NameFr = "Comores" }
    


);

            modelBuilder.Entity<Organization>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReelsRequest>()
                .HasOne(r => r.Opportunity)
                .WithMany()
                .HasForeignKey(r => r.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade); // OK

            modelBuilder.Entity<ReelsRequest>()
                .HasOne(r => r.Organization)
                .WithMany()
                .HasForeignKey(r => r.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict); // ← منع الـ cascade هنا
        }


    }
}

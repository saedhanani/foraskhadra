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
        public DbSet<ChecklistItem> ChecklistItems { get; set; }
        public DbSet<RecentView> RecentViews { get; set; }
        public DbSet<CycleWatch> CycleWatches { get; set; }
        public DbSet<ProgramInterest> ProgramInterests { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<FlagshipProgram> FlagshipPrograms { get; set; }
        public DbSet<ImpactStat> ImpactStats { get; set; }
        public DbSet<PushSubscription> PushSubscriptions { get; set; }
        public DbSet<OpportunityDailyStat> OpportunityDailyStats { get; set; }
        public DbSet<Field> Fields { get; set; }
        public DbSet<PublishRequest> PublishRequests { get; set; }
        public DbSet<PageViewDailyStat> PageViewDailyStats { get; set; }
        public DbSet<TrafficSourceDailyStat> TrafficSourceDailyStats { get; set; }
        public DbSet<SiteEventDailyStat> SiteEventDailyStats { get; set; }
        public DbSet<EligibilityProfile> EligibilityProfiles { get; set; }
        public DbSet<LandingPage> LandingPages { get; set; }
        public DbSet<OpportunityReport> OpportunityReports { get; set; }
        public DbSet<OrganizationFollow> OrganizationFollows { get; set; }

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
                    entry.Entity.UpdatedAt = DateTime.Now;
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

            // طلبات النشر من بوابة المنظمات: حذف الفرصة يفكّ الربط ولا يحذف الطلب
            modelBuilder.Entity<PublishRequest>().HasIndex(r => r.Status);
            modelBuilder.Entity<PublishRequest>()
                .HasOne(r => r.Opportunity)
                .WithMany()
                .HasForeignKey(r => r.OpportunityId)
                .OnDelete(DeleteBehavior.SetNull);

            // التحليلات اليومية: صف واحد لكل (يوم، مفتاح)
            modelBuilder.Entity<PageViewDailyStat>().HasIndex(r => new { r.Day, r.Path, r.Culture }).IsUnique();
            modelBuilder.Entity<TrafficSourceDailyStat>().HasIndex(r => new { r.Day, r.Source }).IsUnique();
            modelBuilder.Entity<SiteEventDailyStat>().HasIndex(r => new { r.Day, r.Event, r.Label }).IsUnique();

            // ملف الأهلية (بوصلة): واحد لكل مستخدم، يُحذف معه
            modelBuilder.Entity<EligibilityProfile>().HasIndex(p => p.UserId).IsUnique();

            // صفحات النية: الرابط فريد
            modelBuilder.Entity<LandingPage>().HasIndex(p => p.Slug).IsUnique();
            modelBuilder.Entity<EligibilityProfile>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<EligibilityProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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
                .HasIndex(n => n.PushedAt);

            // إشعارات الدفع: اشتراكات المستخدم تُحذف معه
            modelBuilder.Entity<PushSubscription>()
                .HasIndex(s => s.UserId);
            modelBuilder.Entity<PushSubscription>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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

            // رفيق التقديم: البنود تُحذف مع إلغاء حفظ الفرصة
            modelBuilder.Entity<ChecklistItem>()
                .HasIndex(c => c.SavedOpportunityId);
            modelBuilder.Entity<ChecklistItem>()
                .HasOne(c => c.SavedOpportunity)
                .WithMany(s => s.Checklist)
                .HasForeignKey(c => c.SavedOpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            // "تابعت مؤخرًا": صف واحد لكل (مستخدم، فرصة)؛ جهة المستخدم بلا cascade كالمعتاد
            modelBuilder.Entity<RecentView>()
                .HasIndex(r => new { r.UserId, r.OpportunityId })
                .IsUnique();
            modelBuilder.Entity<RecentView>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.ClientCascade);
            modelBuilder.Entity<RecentView>()
                .HasOne(r => r.Opportunity)
                .WithMany()
                .HasForeignKey(r => r.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            // مراقبة الدورة القادمة: صف واحد لكل (مستخدم، فرصة)؛ جهة المستخدم بلا cascade كالمعتاد
            modelBuilder.Entity<CycleWatch>()
                .HasIndex(w => new { w.UserId, w.OpportunityId })
                .IsUnique();
            modelBuilder.Entity<CycleWatch>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.ClientCascade);
            modelBuilder.Entity<CycleWatch>()
                .HasOne(w => w.Opportunity)
                .WithMany()
                .HasForeignKey(w => w.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            // اهتمام ببرنامج رائد: صف واحد لكل (مستخدم، برنامج)؛ يُحذف مع حذف البرنامج
            modelBuilder.Entity<ProgramInterest>()
                .HasIndex(p => new { p.UserId, p.ProgramId })
                .IsUnique();
            modelBuilder.Entity<ProgramInterest>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.ClientCascade);
            modelBuilder.Entity<ProgramInterest>()
                .HasOne(p => p.Program)
                .WithMany()
                .HasForeignKey(p => p.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            // الإحالات: فهرس على الرمز (التفرّد يضمنه ReferralService عند التوليد) وعلى الداعي
            // إحصاء يومي لكل فرصة (نقرات التقديم عبر /go/): صف واحد لكل (فرصة، يوم)
            modelBuilder.Entity<OpportunityDailyStat>().HasKey(s => new { s.OpportunityId, s.Day });

            // بلاغات المشاكل (3.1): تُحذف مع الفرصة؛ فهرس (الفرصة، الحالة) لعدّ المفتوح عند عرض التفاصيل
            modelBuilder.Entity<OpportunityReport>()
                .HasIndex(r => new { r.OpportunityId, r.Status });
            modelBuilder.Entity<OpportunityReport>()
                .HasOne(r => r.Opportunity)
                .WithMany()
                .HasForeignKey(r => r.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            // متابعة منظمة (3.2): صف واحد لكل (عضو، منظمة)؛ جهة المستخدم بلا cascade كالمعتاد
            modelBuilder.Entity<OrganizationFollow>()
                .HasIndex(f => new { f.UserId, f.OrganizationId })
                .IsUnique();
            modelBuilder.Entity<OrganizationFollow>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.ClientCascade);
            modelBuilder.Entity<OrganizationFollow>()
                .HasOne(f => f.Organization)
                .WithMany()
                .HasForeignKey(f => f.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>().HasIndex(u => u.ReferralCode);
            modelBuilder.Entity<ApplicationUser>().HasIndex(u => u.ReferredByUserId);

            modelBuilder.Entity<SavedSearch>()
                .HasIndex(s => s.UserId);
            modelBuilder.Entity<SavedSearch>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.ClientCascade);

            // المجالات: قائمة ثابتة (FieldSeed) وجدول ربط OpportunityFields؛ وفهرس على مفتاح البرنامج المتكرر
            modelBuilder.Entity<Field>().HasIndex(f => f.Key).IsUnique();
            modelBuilder.Entity<Opportunity>()
                .HasMany(o => o.Fields)
                .WithMany(f => f.Opportunities)
                .UsingEntity(j => j.ToTable("OpportunityFields"));
            modelBuilder.Entity<Field>().HasData(FieldSeed.All.Select(f => new { f.Id, f.Key, f.NameAr, f.NameEn, f.NameFr, f.SortOrder }));
            modelBuilder.Entity<Opportunity>().HasIndex(o => o.ProgramKey);

            modelBuilder.Entity<Country>().HasData(

    // Asia
    new Country { Id = 1, IsoCode = "XW", NameAr = "كل دول العالم", NameEn = "All World", NameFr = "Tous les pays du monde" },
    new Country { Id = 2, IsoCode = "XA", NameAr = "الوطن العربي ", NameEn = "The Arab World", NameFr = "Le Monde arabe" },
    new Country { Id = 3, IsoCode = "PS", NameAr = "فلسطين", NameEn = "Palestine", NameFr = "Palestine" },
    new Country { Id = 4, IsoCode = "JO", NameAr = "الأردن", NameEn = "Jordan", NameFr = "Jordanie" },
    new Country { Id = 5, IsoCode = "SY", NameAr = "سوريا", NameEn = "Syria", NameFr = "Syrie" },
    new Country { Id = 6, IsoCode = "LB", NameAr = "لبنان", NameEn = "Lebanon", NameFr = "Liban" },
    new Country { Id = 7, IsoCode = "IQ", NameAr = "العراق", NameEn = "Iraq", NameFr = "Irak" },
    new Country { Id = 8, IsoCode = "SA", NameAr = "السعودية", NameEn = "Saudi Arabia", NameFr = "Arabie saoudite" },
    new Country { Id = 9, IsoCode = "KW", NameAr = "الكويت", NameEn = "Kuwait", NameFr = "Koweït" },
    new Country { Id = 10, IsoCode = "QA", NameAr = "قطر", NameEn = "Qatar", NameFr = "Qatar" },
    new Country { Id = 11, IsoCode = "BH", NameAr = "البحرين", NameEn = "Bahrain", NameFr = "Bahreïn" },
    new Country { Id = 12, IsoCode = "AE", NameAr = "الإمارات العربية المتحدة", NameEn = "United Arab Emirates", NameFr = "Émirats arabes unis" },
    new Country { Id = 13, IsoCode = "OM", NameAr = "عُمان", NameEn = "Oman", NameFr = "Oman" },
    new Country { Id = 14, IsoCode = "YE", NameAr = "اليمن", NameEn = "Yemen", NameFr = "Yémen" },

    // Africa
    new Country { Id = 15, IsoCode = "EG", NameAr = "مصر", NameEn = "Egypt", NameFr = "Égypte" },
    new Country { Id = 16, IsoCode = "SD", NameAr = "السودان", NameEn = "Sudan", NameFr = "Soudan" },
    new Country { Id = 17, IsoCode = "LY", NameAr = "ليبيا", NameEn = "Libya", NameFr = "Libye" },
    new Country { Id = 18, IsoCode = "TN", NameAr = "تونس", NameEn = "Tunisia", NameFr = "Tunisie" },
    new Country { Id = 19, IsoCode = "DZ", NameAr = "الجزائر", NameEn = "Algeria", NameFr = "Algérie" },
    new Country { Id = 20, IsoCode = "MA", NameAr = "المغرب", NameEn = "Morocco", NameFr = "Maroc" },
    new Country { Id = 21, IsoCode = "MR", NameAr = "موريتانيا", NameEn = "Mauritania", NameFr = "Mauritanie" },
    new Country { Id = 22, IsoCode = "DJ", NameAr = "جيبوتي", NameEn = "Djibouti", NameFr = "Djibouti" },
    new Country { Id = 23, IsoCode = "SO", NameAr = "الصومال", NameEn = "Somalia", NameFr = "Somalie" },
    new Country { Id = 24, IsoCode = "KM", NameAr = "جزر القمر", NameEn = "Comoros", NameFr = "Comores" }
    


);

            modelBuilder.Entity<Organization>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReelsRequest>()
                .HasOne(r => r.Opportunity)
                .WithMany(o => o.ReelsRequests) // كانت WithMany() فأنشأ EF عمود ظل OpportunityId1
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace N7.Models
{
    public class ApplicationContext : IdentityDbContext<AppUser>
    {
        public DbSet<AppCollection> AppCollections { get; set; }
        public DbSet<AppItem> AppItems { get; set; }
        public DbSet<AppComment> AppComments { get; set; }
        public DbSet<AppLike> AppLikes { get; set; }
        public DbSet<AppTheme> AppThemes { get; set; }
        public DbSet<AppHeader> AppHeaders { get; set; }
        public DbSet<AppBool> AppBools { get; set; }
        public DbSet<AppDate> AppDates { get; set; }
        public DbSet<AppNumber> AppNumbers { get; set; }
        public DbSet<AppString> AppStrings { get; set; }
        public DbSet<AppText> AppTexts { get; set; }        
        public DbSet<AppTag> AppTags { get; set; }    
        public DbSet<AppTagCloudRecord> AppTagCloudRecords {get; set;}
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AppCollection>()
                .HasOne(x => x.AppUser)
                .WithMany(c => c.AppCollections)
                .OnDelete(DeleteBehavior.Cascade);            
        }

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        
    }
}

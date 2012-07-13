using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using Highway.Data.Tests.TestDomain;

namespace Highway.Data.EntityFramework.Tests.Mapping
{
    public class QuxMap : EntityTypeConfiguration<Qux>
    {
        public QuxMap()
        {
            this.ToTable("Quxs");
            this.HasKey(x => x.Id);
            this.Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            this.Property(x => x.Name).IsOptional();
        }
    }
}
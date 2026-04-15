using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public sealed class FarHorizonsModel : DataModelBase
{
    public override void OnModelCreating(ServerDbContext dbContext, ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FarHorizonsProfile>(entity =>
        {
            entity.HasOne(e => e.Profile)
                .WithOne(p => p.FarHorizonsProfile)
                .HasForeignKey<FarHorizonsProfile>(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProfileId)
                .IsUnique();

            entity.HasOne(e => e.Symspeech)
                .WithOne()
                .HasForeignKey<FarHorizonsProfile>(e => e.SymspeechId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.SiliconSymspeech)
                .WithOne()
                .HasForeignKey<FarHorizonsProfile>(e => e.SiliconSymspeechId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
    
    public class FarHorizonsProfile
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public virtual Profile Profile { get; set; } = null!;

        public int? SymspeechId { get; set; }
        public SymspeechDTO? Symspeech { get; set; }

        public int? SiliconSymspeechId { get; set; }
        public SymspeechDTO? SiliconSymspeech { get; set; }
    }

    [Table("fh_symspeech")]
    public class SymspeechDTO
    {
        [Key] public int Id { get; set; }

        public string Voice { get; set; } = string.Empty;
        public int Pitch { get; set; }
        public float Speed { get; set; }
        public float Pause { get; set; }
        public int Polyphony { get; set; }
        public float Volume { get; set; }
    }
}
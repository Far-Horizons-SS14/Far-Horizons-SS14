using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public partial class Profile
{
    public int? SymspeechId { get; set; }
    public FarHorizonsModel.SymspeechDTO? Symspeech { get; set; } = null!;

    public int? SiliconSymspeechId { get; set; }
    public FarHorizonsModel.SymspeechDTO? SiliconSymspeech { get; set; } = null!;
}

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
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasOne(p => p.Symspeech)
                .WithOne()
                .HasForeignKey<Profile>(p => p.SymspeechId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.SiliconSymspeech)
                .WithOne()
                .HasForeignKey<Profile>(p => p.SiliconSymspeechId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
    
    public class FarHorizonsProfile
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public virtual Profile Profile { get; set; } = null!;
    }

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
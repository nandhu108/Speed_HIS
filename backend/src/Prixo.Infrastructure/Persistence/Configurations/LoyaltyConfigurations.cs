using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Loyalty;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Loyalty card engine (front-office extension — see docs/modules/loyalty.md). Schema: loyalty.

public class LoyaltyCardConfiguration : IEntityTypeConfiguration<LoyaltyCard>
{
    public void Configure(EntityTypeBuilder<LoyaltyCard> builder)
    {
        builder.ToTable("loyalty_cards", "loyalty");
        builder.Property(c => c.CardNumber).HasMaxLength(30);
        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.DeactivateReason).HasMaxLength(500);

        builder.HasIndex(c => new { c.BranchId, c.CardNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(c => c.PatientId);

        builder.HasOne(c => c.Patient).WithMany()
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.LoyaltyCardType).WithMany()
            .HasForeignKey(c => c.LoyaltyCardTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(c => c.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_loyalty_cards_duration", "duration_months > 0"));
    }
}

public class LoyaltyCardMemberConfiguration : IEntityTypeConfiguration<LoyaltyCardMember>
{
    public void Configure(EntityTypeBuilder<LoyaltyCardMember> builder)
    {
        builder.ToTable("loyalty_card_members", "loyalty");
        builder.HasIndex(m => new { m.LoyaltyCardId, m.PatientId }).IsUnique().HasFilter("is_deleted = false AND removed_at IS NULL");

        builder.HasOne(m => m.LoyaltyCard).WithMany()
            .HasForeignKey(m => m.LoyaltyCardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Patient).WithMany()
            .HasForeignKey(m => m.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.MemberRelationTag).WithMany()
            .HasForeignKey(m => m.MemberRelationTagId).OnDelete(DeleteBehavior.Restrict);
    }
}

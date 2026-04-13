using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderContext.Domain;

namespace OrderContext.Infratructure.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Configure Email using Value Conversion
        builder.Property(c => c.Email)
            .HasConversion(
                email => email.Value,
                value => Email.FromDatabase(value))
            .HasMaxLength(254)
            .IsRequired();
    }
}

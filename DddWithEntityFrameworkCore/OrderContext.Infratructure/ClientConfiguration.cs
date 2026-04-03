using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderContext.Infratructure;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);

        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Value)
                 .IsRequired()
                 .HasColumnName("Email");
        });
    }
}

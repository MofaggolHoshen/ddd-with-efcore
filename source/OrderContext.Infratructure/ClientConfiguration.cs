using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderContext.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderContext.Infratructure;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);

        builder.OwnsOne<Email>(c => c.Email);

        builder.Navigation(c => c.Email).IsRequired();
    }
}

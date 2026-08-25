using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace bitewing.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Classification> Classifications => Set<Classification>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<TicketEvent> TicketEvents => Set<TicketEvent>();
    public DbSet<SpotCheck> SpotChecks => Set<SpotCheck>();
    public DbSet<Settings> Settings => Set<Settings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Ticket>(entity =>
        {
            entity.HasOne(t => t.Assignee)
                .WithMany()
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-reference: deleting a ticket must not cascade into whatever
            // ticket it links back to.
            entity.HasOne(t => t.RelatedTicket)
                .WithMany()
                .HasForeignKey(t => t.RelatedTicketId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Classification>(entity =>
        {
            entity.HasIndex(c => c.TicketId).IsUnique();

            entity.HasOne(c => c.Ticket)
                .WithOne(t => t.Classification)
                .HasForeignKey<Classification>(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SpotCheck>(entity =>
        {
            entity.HasIndex(s => s.TicketId).IsUnique();

            entity.HasOne(s => s.Ticket)
                .WithOne(t => t.SpotCheck)
                .HasForeignKey<SpotCheck>(s => s.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Agent)
                .WithMany()
                .HasForeignKey(s => s.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Note>(entity =>
        {
            entity.HasOne(n => n.Ticket)
                .WithMany(t => t.Notes)
                .HasForeignKey(n => n.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Author)
                .WithMany()
                .HasForeignKey(n => n.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TicketEvent>(entity =>
        {
            entity.HasOne(e => e.Ticket)
                .WithMany(t => t.Events)
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Actor)
                .WithMany()
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
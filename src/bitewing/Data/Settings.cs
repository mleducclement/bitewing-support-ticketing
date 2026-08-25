namespace bitewing.Data;

// Single row. Editable by Team Lead, visible to all agents.
public class Settings
{
    public int Id { get; set; }

    public int BlockedExpiryDays { get; set; } = 14;
}
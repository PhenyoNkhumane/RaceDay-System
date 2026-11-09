namespace RaceDay.Api.Models;

public class Event
{
    public int Id { get; set; }
    public int OrganiserId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public decimal DistanceKm { get; set; }
    public required string EventType { get; set; }
    public required string VenueName { get; set; }
    public required string City { get; set; }
    public required string Province { get; set; }
    public required string Status { get; set; }
    public string? BannerUrl { get; set; }

    public Organiser? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

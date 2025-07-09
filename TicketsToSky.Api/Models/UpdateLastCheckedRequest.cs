namespace TicketsToSky.Api.Models;

using System.ComponentModel.DataAnnotations;

public class RequestOnlyWithId
{
    [Required]
    public Guid Id { get; set; }
}
namespace TicketsToSky.Api.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using TicketsToSky.Api.Models;
using TicketsToSky.Api.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("/health")]

public class HealthController() : ControllerBase
{

    [HttpGet]
    public Task ReplyHealth()
    {
        return Task.FromResult(Ok(new { status = "Healthy" }));
    }
}
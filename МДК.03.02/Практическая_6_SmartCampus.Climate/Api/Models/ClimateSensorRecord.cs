using System.ComponentModel.DataAnnotations;

namespace SmartCampus.Climate.Api.Models;

// Сущность из задания (вариант 3, модуль «Умный климат-контроль»)
public class ClimateSensorRecord
{
    public int Id { get; set; }
    [Required] public string? AuditoriumCode { get; set; }
    public float Temperature { get; set; }
    public float Humidity { get; set; }
}

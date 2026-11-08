using Microsoft.AspNetCore.Mvc;
using SmartCampus.Climate.Api.Data;
using SmartCampus.Climate.Api.Models;

namespace SmartCampus.Climate.Api.Controllers;

[ApiController]
[Route("api/v1/climate/sensor")]
public class ClimateController : ControllerBase
{
    private readonly ClimateStore _store;
    public ClimateController(ClimateStore store) => _store = store;

    // POST /api/v1/climate/sensor : при неверной модели [ApiController] сам вернёт 400
    [HttpPost]
    public ActionResult<ClimateSensorRecord> Post(ClimateSensorRecord record) =>
        Ok(_store.Add(record));

    // GET /api/v1/climate/sensor
    [HttpGet]
    public ActionResult<List<ClimateSensorRecord>> Get() => Ok(_store.GetAll());
}

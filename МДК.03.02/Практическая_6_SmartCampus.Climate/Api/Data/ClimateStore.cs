using SmartCampus.Climate.Api.Models;

namespace SmartCampus.Climate.Api.Data;

// Хранилище показаний. У каждого экземпляра приложения оно своё и начинается пустым
public class ClimateStore
{
    private readonly List<ClimateSensorRecord> _items = new();
    private readonly object _lock = new();
    private int _nextId = 1;

    public ClimateSensorRecord Add(ClimateSensorRecord r)
    {
        lock (_lock) { r.Id = _nextId++; _items.Add(r); return r; }
    }

    public List<ClimateSensorRecord> GetAll()
    {
        lock (_lock) return _items.ToList();
    }
}

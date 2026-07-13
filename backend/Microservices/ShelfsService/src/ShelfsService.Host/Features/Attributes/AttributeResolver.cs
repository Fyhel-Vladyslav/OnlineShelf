using ShelfsService.src.ShelfsService.Common.Interfaces;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ShelfsService.src.ShelfsService.Common.DTOs.Items;

namespace ShelfsService.src.ShelfsService.Host.Features.Attributes;

public class AttributeResolver : IAttributeResolver
{
    // Ключ - AttributeId, Значення - Весь об'єкт типу з його списком значень
    private readonly ConcurrentDictionary<int, AttributeTypeDto> _cache;

    public AttributeResolver(IServiceScopeFactory scopeFactory)
    {
        _cache = new ConcurrentDictionary<int, AttributeTypeDto>();
        InitializeCache(scopeFactory);
    }

    private void InitializeCache(IServiceScopeFactory scopeFactory)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IItemRepository>();

            // 1. Отримуємо назви типів
            var types = repository.GetAttributesAsync().GetAwaiter().GetResult();
            // 2. Отримуємо всі значення
            var values = repository.GetAttributesValuesAsync().GetAwaiter().GetResult();

            foreach (var type in types)
            {
                var dto = new AttributeTypeDto
                {
                    AttributeId = type.Id,
                    TypeName = type.Name,
                    Options = values
                        .Where(v => v.AttributeId == type.Id)
                        .Select(v => new AttributeOption { Key = v.AttributeKey, Value = v.AttributeValue })
                        .ToList()
                };
                _cache[type.Id] = dto;
            }
        }
    }

    // Повертає все одним махом (ідеально для DropDowns на сторінці)
    public IEnumerable<AttributeTypeDto> GetAllData() => _cache.Values;

    // Пошук конкретного текстового значення
    public string GetValue(int attributeId, int attributeKey)
    {
        if (_cache.TryGetValue(attributeId, out var typeDto))
        {
            return typeDto.Options.FirstOrDefault(o => o.Key == attributeKey)?.Value ?? string.Empty;
        }
        return string.Empty;
    }

}
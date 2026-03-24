using Bogus;
using Service.Api.Entities;

namespace Service.Api.Generator;

/// <summary>
/// Генератор сотрудников компании со случайными свойствами
/// </summary>
public static class EmployeeGenerator
{
    /// <summary>
    /// Справочник категорий профессий 
    /// </summary>
    private static readonly string[] _professions = { "Developer", "Manager", "Analyst", "Designer", "QA" };

    /// <summary>
    /// Словарь связи категории должности с заработной платой
    /// </summary>
    private static readonly Dictionary<string, (decimal Min, decimal Max)> _salaryRanges = new()
    {
        ["Junior"] = (30000m, 60000m),
        ["Middle"] = (60000m, 120000m),
        ["Senior"] = (120000m, 200000m),
        ["Lead"] = (150000m, 250000m)
    };

    /// <summary>
    /// Справочник категорий суффиксов должностей
    /// </summary>
    private static readonly string[] _suffixes = [.. _salaryRanges.Keys];

    private static readonly Faker<Employee> _faker = new Faker<Employee>("ru")
        .RuleFor(e => e.Id, f => f.IndexFaker + 1)
        .RuleFor(e => e.FullName, f => f.Name.FullName())
        .RuleFor(e => e.Post, f => f.PickRandom(_suffixes) + " " + f.PickRandom(_professions))
        .RuleFor(e => e.Department, f => f.Commerce.Department())
        .RuleFor(e => e.HireDate, f => DateOnly.FromDateTime(f.Date.Past(10)))
        .RuleFor(e => e.Salary, (f, e) =>
        {
            var suffix = _suffixes.FirstOrDefault(s => e.Post.Contains(s));
            if (suffix != null)
            {
                var (min, max) = _salaryRanges[suffix];
                return Math.Round(f.Random.Decimal(min, max), 2);
            }
            return Math.Round(f.Random.Decimal(40000m, 100000m), 2);
        })
        .RuleFor(e => e.Email, f => f.Internet.Email())
        .RuleFor(e => e.Phone, f => f.Phone.PhoneNumber("+7(###)###-##-##"))
        .RuleFor(e => e.IsFired, f => f.Random.Bool(0.2f))
        .RuleFor(e => e.FireDate, (f, e) => e.IsFired ? DateOnly.FromDateTime(f.Date.Past(1)) : null);

    /// <summary>
    /// Метод генерации СК
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <returns>Сотрудник компании</returns>
    public static Employee Generate(int id)
    {
        var employee = _faker.Generate();
        employee.Id = id;
        return employee;
    }
}

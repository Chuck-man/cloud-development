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
    /// Справочник категорий суффиксов должностей
    /// </summary>
    private static readonly string[] _suffexies = { "Junior", "Middle", "Senior", "Lead" };

    private static readonly Faker<Employee> _faker = new Faker<Employee>("ru")
        .RuleFor(e => e.Id, f => f.IndexFaker + 1)
        .RuleFor(e => e.FullName, f => f.Name.FullName())
        .RuleFor(e => e.Post, f => f.PickRandom(_suffexies) + " " + f.PickRandom(_professions))
        .RuleFor(e => e.Department, f => f.Commerce.Department())
        .RuleFor(e => e.HireDate, f => DateOnly.FromDateTime(f.Date.Past(10)))
        .RuleFor(e => e.Salary, (f, e) => CalculateSalary(e.Post))
        .RuleFor(e => e.Email, f => f.Internet.Email())
        .RuleFor(e => e.Phone, f => f.Phone.PhoneNumber("+7(###)###-##-##"))
        .RuleFor(e => e.IsFired, f => f.Random.Bool(0.2f))
        .RuleFor(e => e.FireDate, (f, e) => e.IsFired ? DateOnly.FromDateTime(f.Date.Past(1)) : null);

    /// <summary>
    /// Метод вычисления оклада в зависимости от суффикса должности
    /// </summary>
    /// <param name="position">Должность</param>
    /// <returns>Оклад</returns>
    private static decimal CalculateSalary(string position)
    {
        Faker faker = new();

        return position switch
        {
            var p when p.Contains("Junior") =>
                Math.Round(faker.Random.Decimal(30000m, 60000m), 2),
            var p when p.Contains("Middle") =>
                Math.Round(faker.Random.Decimal(60000m, 120000m), 2),
            var p when p.Contains("Senior") =>
                Math.Round(faker.Random.Decimal(120000m, 200000m), 2),
            var p when p.Contains("Lead") =>
                Math.Round(faker.Random.Decimal(150000m, 250000m), 2),
            _ => Math.Round(faker.Random.Decimal(40000m, 100000m), 2)
        };
    }

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

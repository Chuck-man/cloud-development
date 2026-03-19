using System.Text.Json.Serialization;

namespace Service.Api.Entities;

/// <summary>
/// Сотрудник компании
/// </summary>
public class Employee
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// ФИО
    /// </summary>
    [JsonPropertyName("fullName")]
    public required string FullName { get; set; }

    /// <summary>
    /// Должность
    /// </summary>
    [JsonPropertyName("post")]
    public required string Post { get; set; }

    /// <summary>
    /// Отдел
    /// </summary>
    [JsonPropertyName("department")]
    public required string Department { get; set; }

    /// <summary>
    /// Дата приема
    /// </summary>
    [JsonPropertyName("hireDate ")]
    public required DateOnly HireDate { get; set; }

    /// <summary>
    /// Оклад
    /// </summary>
    [JsonPropertyName("salary")]
    public required decimal Salary { get; set; }

    /// <summary>
    /// Электронная почта
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    /// <summary>
    /// Номер телефона
    /// </summary>
    [JsonPropertyName("phone")]
    public required string Phone { get; set; }

    /// <summary>
    /// Индикатор увольнения
    /// </summary>
    [JsonPropertyName("isFired")]
    public required bool IsFired { get; set; }

    /// <summary>
    /// Дата увольнения
    /// </summary>
    [JsonPropertyName("fireDate ")]
    public DateOnly? FireDate { get; set; }
}

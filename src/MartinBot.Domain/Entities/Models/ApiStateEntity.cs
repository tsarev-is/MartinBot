namespace MartinBot.Domain.Entities.Models;

/// <summary>
/// Key/value store for small bot state that must survive restarts (primarily the EXMO nonce).
/// </summary>
public sealed class ApiStateEntity
{
    public ApiStateEntity(string key, long value, DateTimeOffset updatedAt)
    {
        Key = key;
        Value = value;
        UpdatedAt = updatedAt;
    }

    public string Key { get; private set; }

    public long Value { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}

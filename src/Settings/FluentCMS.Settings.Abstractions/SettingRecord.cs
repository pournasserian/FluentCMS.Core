namespace FluentCMS.Settings.Abstractions;

public sealed record SettingRecord(
    string ValueType,
    string ValueJson);
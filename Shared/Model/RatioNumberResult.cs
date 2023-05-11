namespace Shared.Model;

public readonly record struct RatioNumberResult {
    public double Value { get; init; }
    public bool HasValue { get; init; }
    public bool IsRatio { get; init; }

    public RatioNumberResult(double value, bool hasValue, bool isRatio) {
        Value = value;
        HasValue = hasValue;
        IsRatio = isRatio;
    }
}

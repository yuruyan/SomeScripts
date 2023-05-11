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

    public void Deconstruct(out double value, out bool hasValue, out bool isRatio) {
        value = Value;
        hasValue = HasValue;
        isRatio = IsRatio;
    }
}

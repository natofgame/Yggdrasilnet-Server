namespace Yggdrasilnet.Server.Core.Behavior.Core;

internal static class BehaviorValidation {
    public static void EnsureFinite(float value, string key) {
        if (!float.IsFinite(value)) {
            throw new InvalidDataException($"{key} must be a finite number.");
        }
    }

    public static void EnsurePositive(float value, string key) {
        if (!float.IsFinite(value) || value <= 0f) {
            throw new InvalidDataException($"{key} must be > 0.");
        }
    }

    public static void EnsureNonNegative(float value, string key) {
        if (!float.IsFinite(value) || value < 0f) {
            throw new InvalidDataException($"{key} must be >= 0.");
        }
    }

    public static void EnsureFinite(params (float Value, string Key)[] values) {
        foreach (var (value, key) in values)
            EnsureFinite(value, key);
    }

    public static void EnsureNonNegative(params (float Value, string Key)[] values) {
        foreach (var (value, key) in values)
            EnsureNonNegative(value, key);
    }
}

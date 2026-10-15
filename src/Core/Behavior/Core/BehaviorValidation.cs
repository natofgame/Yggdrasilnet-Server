namespace Yggdrasilnet.Server.Core.Behavior.Core;

internal static class BehaviorValidation {
    /// <summary>
    ///     Ensures that the given value is finite.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="key"></param>
    /// <exception cref="InvalidDataException"></exception>
    public static void EnsureFinite(float value, string key) {
        if (!float.IsFinite(value)) {
            throw new InvalidDataException($"{key} must be a finite number.");
        }
    }

    /// <summary>
    ///     Ensures that the given value is positive.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="key"></param>
    /// <exception cref="InvalidDataException"></exception>
    public static void EnsurePositive(float value, string key) {
        if (!float.IsFinite(value) || value <= 0f) {
            throw new InvalidDataException($"{key} must be > 0.");
        }
    }

    /// <summary>
    ///     Ensures that the given value is non-negative.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="key"></param>
    /// <exception cref="InvalidDataException"></exception>
    public static void EnsureNonNegative(float value, string key) {
        if (!float.IsFinite(value) || value < 0f) {
            throw new InvalidDataException($"{key} must be >= 0.");
        }
    }

    /// <summary>
    ///     Ensures that all given values are finite.
    /// </summary>
    /// <param name="values"></param>
    public static void EnsureFinite(params (float Value, string Key)[] values) {
        foreach (var (value, key) in values)
            EnsureFinite(value, key);
    }

    /// <summary>
    ///     Ensures that all given values are positive.
    /// </summary>
    /// <param name="values"></param>
    public static void EnsureNonNegative(params (float Value, string Key)[] values) {
        foreach (var (value, key) in values)
            EnsureNonNegative(value, key);
    }
}
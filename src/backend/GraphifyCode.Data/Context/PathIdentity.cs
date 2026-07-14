using System;
using System.IO;

namespace GraphifyCode.Data.Context;

public static class PathIdentity
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    public static bool IsValidSegment(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var trimmed = name.Trim();
        if (trimmed is "." or "..")
        {
            return false;
        }

        return trimmed.IndexOfAny(InvalidFileNameChars) < 0;
    }

    public static void EnsureValidSegment(string name, string fieldName)
    {
        if (!IsValidSegment(name))
        {
            throw new ArgumentException(
                $"'{fieldName}' value '{name}' is not a valid path identity segment.",
                fieldName);
        }
    }

    public static bool NamesEqual(string? left, string? right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}

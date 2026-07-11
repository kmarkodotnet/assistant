using System.Text.Json;

namespace FamilyOs.Application.Ai;

/// <summary>
/// Minimal JSON Schema (draft 2020-12) validator covering exactly the keywords the 3
/// whitelisted tool schemas use (ai-pipeline.md §11.2/§11.3 step 3): required, type,
/// enum, minLength/maxLength, minimum/maximum, additionalProperties:false. Not a general
/// schema engine on purpose — keeps the strict-JSON protocol dependency-free.
/// </summary>
public static class JsonSchemaLiteValidator
{
    public static bool TryValidate(JsonElement schema, JsonElement instance, out string? error)
    {
        error = null;

        if (instance.ValueKind != JsonValueKind.Object)
        {
            error = "Az argumentumoknak JSON objektumnak kell lenniük.";
            return false;
        }

        var properties = schema.TryGetProperty("properties", out var p) ? p : default;

        if (schema.TryGetProperty("required", out var requiredEl) && requiredEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var reqName in requiredEl.EnumerateArray())
            {
                var name = reqName.GetString();
                if (name is not null && !instance.TryGetProperty(name, out _))
                {
                    error = $"Hiányzó kötelező mező: {name}.";
                    return false;
                }
            }
        }

        if (schema.TryGetProperty("additionalProperties", out var addProps)
            && addProps.ValueKind == JsonValueKind.False
            && properties.ValueKind == JsonValueKind.Object)
        {
            foreach (var member in instance.EnumerateObject())
            {
                if (!properties.TryGetProperty(member.Name, out _))
                {
                    error = $"Ismeretlen mező: {member.Name}.";
                    return false;
                }
            }
        }

        if (properties.ValueKind == JsonValueKind.Object)
        {
            foreach (var propSchema in properties.EnumerateObject())
            {
                if (!instance.TryGetProperty(propSchema.Name, out var value))
                    continue; // optional and absent

                if (!TryValidateValue(propSchema.Value, value, propSchema.Name, out error))
                    return false;
            }
        }

        return true;
    }

    private static bool TryValidateValue(JsonElement propSchema, JsonElement value, string propName, out string? error)
    {
        error = null;

        if (propSchema.TryGetProperty("enum", out var enumEl) && enumEl.ValueKind == JsonValueKind.Array)
        {
            var allowed = enumEl.EnumerateArray().Select(e => e.GetString()).ToList();
            var actual = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            if (!allowed.Contains(actual))
            {
                error = $"'{propName}' érvénytelen érték: {actual}.";
                return false;
            }
        }

        var typeKind = ResolveType(propSchema);

        switch (typeKind)
        {
            case "string":
                if (value.ValueKind != JsonValueKind.String)
                {
                    error = $"'{propName}' mezőnek szövegnek kell lennie.";
                    return false;
                }
                var s = value.GetString() ?? string.Empty;
                if (propSchema.TryGetProperty("minLength", out var minLen) && s.Length < minLen.GetInt32())
                {
                    error = $"'{propName}' túl rövid.";
                    return false;
                }
                if (propSchema.TryGetProperty("maxLength", out var maxLen) && s.Length > maxLen.GetInt32())
                {
                    error = $"'{propName}' túl hosszú.";
                    return false;
                }
                break;

            case "integer":
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var num))
                {
                    error = $"'{propName}' mezőnek egész számnak kell lennie.";
                    return false;
                }
                if (propSchema.TryGetProperty("minimum", out var min) && num < min.GetInt64())
                {
                    error = $"'{propName}' túl kicsi.";
                    return false;
                }
                if (propSchema.TryGetProperty("maximum", out var max) && num > max.GetInt64())
                {
                    error = $"'{propName}' túl nagy.";
                    return false;
                }
                break;

            case "null-or-string":
                if (value.ValueKind is not (JsonValueKind.Null or JsonValueKind.String))
                {
                    error = $"'{propName}' mezőnek szövegnek vagy null-nak kell lennie.";
                    return false;
                }
                break;
        }

        return true;
    }

    private static string? ResolveType(JsonElement propSchema)
    {
        if (!propSchema.TryGetProperty("type", out var typeEl))
            return null;

        if (typeEl.ValueKind == JsonValueKind.String)
            return typeEl.GetString();

        if (typeEl.ValueKind == JsonValueKind.Array)
        {
            var types = typeEl.EnumerateArray().Select(t => t.GetString()).ToList();
            if (types.Contains("null") && types.Contains("string"))
                return "null-or-string";
        }

        return null;
    }
}

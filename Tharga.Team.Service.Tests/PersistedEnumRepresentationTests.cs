using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Tharga.MongoDB;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Every enum persisted by this assembly must be stored by name.
/// </summary>
/// <remarks>
/// The driver's default representation for an enum is <c>Int32</c>, so leaving the attribute off is not a
/// neutral omission — it selects the ordinal, and nothing about the declaration says so. A stored ordinal
/// is correct only while the enum's declaration order never changes; inserting or reordering a member
/// silently re-grades every document already written.
/// <para>
/// Deliberately duplicated from the sibling test in <c>Tharga.Team.MongoDB.Tests</c> rather than shared:
/// each test project guards the entities in its own assembly, so neither needs a reference to the other.
/// </para>
/// </remarks>
public class PersistedEnumRepresentationTests
{
    [Fact]
    public void EveryPersistedEnum_DeclaresStringRepresentation()
    {
        var offenders = PersistedTypes(typeof(ApiKeyEntity).Assembly)
            .SelectMany(EnumProperties)
            .Where(p => !StoresByName(p))
            .Select(p => $"{p.DeclaringType?.Name}.{p.Name}")
            .OrderBy(x => x)
            .ToArray();

        Assert.True(offenders.Length == 0,
            "These persisted enums would be stored as ordinals. Add [BsonRepresentation(BsonType.String)], " +
            "or document on the property why an ordinal is required: " + string.Join(", ", offenders));
    }

    /// <summary>Guards the sweep itself — a filter that silently matches nothing would pass forever.</summary>
    [Fact]
    public void TheSweep_FindsThePersistedEnumsItIsMeantToCheck()
    {
        var found = PersistedTypes(typeof(ApiKeyEntity).Assembly)
            .SelectMany(EnumProperties)
            .Select(p => $"{p.DeclaringType?.Name}.{p.Name}")
            .ToArray();

        Assert.Contains("ApiKeyEntity.AccessLevel", found);
        Assert.Contains("AuditEntryEntity.EventType", found);
        Assert.Contains("AuditEntryEntity.CallerType", found);
        Assert.Contains("AuditEntryEntity.ScopeResult", found);
    }

    private static IEnumerable<Type> PersistedTypes(Assembly assembly)
        => assembly.GetTypes().Where(IsPersisted);

    private static bool IsPersisted(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            var definition = current.IsGenericType ? current.GetGenericTypeDefinition() : current;
            if (definition == typeof(EntityBase))
                return true;
        }

        return false;
    }

    private static IEnumerable<PropertyInfo> EnumProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType).IsEnum);

    private static bool StoresByName(PropertyInfo property)
        => property.GetCustomAttribute<BsonRepresentationAttribute>()?.Representation == BsonType.String;
}

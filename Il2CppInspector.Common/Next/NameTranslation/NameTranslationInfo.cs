using System.Collections.Frozen;

namespace Il2CppInspector.Next.NameTranslation;

public sealed record NameTranslationInfo(
    FrozenDictionary<string, string> NameTranslation,
    FrozenDictionary<string, string> ClassNamespaces 
);
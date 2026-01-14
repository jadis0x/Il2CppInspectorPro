using dnlib.DotNet;
using System.Collections.Frozen;
using System.Diagnostics;

namespace Il2CppInspector.Next.NameTranslation;

public ref struct NameTranslationParserContext
{
    private enum CurrentSection
    {
        None,
        Classes,
        Methods,
        Fields,
        Properties,
        Events,
        Parameters,
        Hashes,
    }

    private CurrentSection _currentSection;
    private bool _reverseOrder;

    private readonly ReadOnlySpan<string> _nameTranslationLines;
    private readonly char _seperator;

    private NameTranslationParserContext(ReadOnlySpan<string> nameTranslationLines, char seperator)
    {
        _currentSection = CurrentSection.None;
        _nameTranslationLines = nameTranslationLines;
        _seperator = seperator;
    }

    public static NameTranslationInfo Parse(ReadOnlySpan<string> nameTranslationLines, char seperator = '⇨')
    {
        var ctx = new NameTranslationParserContext(nameTranslationLines, seperator);
        return ctx.Parse();
    }

    private void HandleMetadataLine(string line)
    {
        switch (line)
        {
            case "#ReverseOrder":
                _reverseOrder = true;
                break;
            case "#Classes":
                _currentSection = CurrentSection.Classes;
                break;
            case "#Methods":
                _currentSection = CurrentSection.Methods;
                break;
            case "#Fields":
                _currentSection = CurrentSection.Fields;
                break;
            case "#Properties":
                _currentSection = CurrentSection.Properties;
                break;
            case "#Events":
                _currentSection = CurrentSection.Events;
                break;
            case "#Parameters":
                _currentSection = CurrentSection.Parameters;
                break;
            // Used in some variations of the standard name translation map.
            // This doesn't seperate data by sections, but is easier to parse overall
            case not null when line.StartsWith("#Hashes"):
                _currentSection = CurrentSection.Hashes;
                break;
            default:
                Debug.Assert(false);
                break;
        }
    }

    private NameTranslationInfo Parse()
    {
        var nameTranslationMap = new Dictionary<string, string>(_nameTranslationLines.Length);
        var namespaceTranslationMap = new Dictionary<string, string>();

        foreach (var line in _nameTranslationLines)
        {
            if (line.StartsWith('#'))
            {
                HandleMetadataLine(line);
                continue;
            }

            var split = line.Split(_seperator, 2);

            var obfuscatedName = split[0];
            var deobfuscatedFullName = split[1].TrimEnd();
            if (!_reverseOrder)
            {
                (obfuscatedName, deobfuscatedFullName) = (deobfuscatedFullName, obfuscatedName);
            }

            string deobfuscatedName;
            string? deobfuscatedNamespace;
            if (_currentSection != CurrentSection.Classes && _currentSection != CurrentSection.Hashes)
            {
                deobfuscatedNamespace = null;
                deobfuscatedName = ParseFullName(deobfuscatedFullName);
            }
            else
            {
                (deobfuscatedName, deobfuscatedNamespace) = ParseFullClassName(deobfuscatedFullName);
            }
            
            // This sometimes happens, just ignore it if so
            if (obfuscatedName == deobfuscatedName)
                continue;

            if (nameTranslationMap.TryGetValue(obfuscatedName, out var alreadySeenName))
            {
                if (alreadySeenName != deobfuscatedName)
                    throw new InvalidDataException(
                        $"Name translation collision detected for name {obfuscatedName}: {alreadySeenName} vs. {deobfuscatedName}");
            }
            else
            {
                nameTranslationMap[obfuscatedName] = deobfuscatedName;

                if (deobfuscatedNamespace != null)
                    namespaceTranslationMap[obfuscatedName] = deobfuscatedNamespace;
            }
        }

        return new NameTranslationInfo(
            nameTranslationMap
                .ToFrozenDictionary(),
            namespaceTranslationMap
                .ToFrozenDictionary());
    }

    private string ParseFullName(string fullName)
        => _currentSection switch
        {
            CurrentSection.Classes => ParseFullClassName(fullName).Item1,
            CurrentSection.Methods or CurrentSection.Properties => ParseFullMemberWithArgumentsName(fullName),
            CurrentSection.Fields or CurrentSection.Events => ParseFullMemberName(fullName),
            CurrentSection.Parameters => ParseFullParameterName(fullName),
            _ => throw new UnreachableException()
        };

    private static (string, string?) ParseFullClassName(string fullName)
    {
        // For the namespace, return everything up until the last part of the fully qualified class name
        var lastNamespaceIdx = fullName.LastIndexOf('.');

        var ns = lastNamespaceIdx == -1
            ? null
            : fullName[..lastNamespaceIdx];

        // If we have a nested class, return that name
        var nestedClassIdx = fullName.LastIndexOf('/') + 1;
        if (nestedClassIdx != 0)
            return (fullName[nestedClassIdx..], ns);

        // Otherwise, if we have a namespace, return the last part of the fully qualified name
        if (lastNamespaceIdx != -1)
            return (fullName[(lastNamespaceIdx + 1)..], ns);

        // Otherwise the class has no namespace, return the full name
        return (fullName, ns);
    }

    // Used for both fields and events
    private static string ParseFullMemberName(string fullName)
    {
        var memberNameStartIdx = fullName.LastIndexOf("::", StringComparison.Ordinal) + 2;
        return fullName[memberNameStartIdx..];
    }

    private static string ParseFullMemberWithArgumentsName(string fullName)
    {
        var methodNameWithParameters = ParseFullMemberName(fullName);
        var parametersStartIdx = methodNameWithParameters.IndexOf('(');
        
        return methodNameWithParameters[..parametersStartIdx];
    }

    private static string ParseFullParameterName(string fullName)
    {
        var parameterNameStartIdx = fullName.LastIndexOf(' ') + 1;
        return fullName[parameterNameStartIdx..];
    }
}
using dnlib.DotNet;
using Il2CppInspector.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Il2CppInspector.Next.NameTranslation;

public readonly struct NameTranslationApplierContext
{
    private readonly NameTranslationInfo _nameTranslationInfo;

    private NameTranslationApplierContext(NameTranslationInfo nameTranslationInfo)
    {
        _nameTranslationInfo = nameTranslationInfo;
    }

    public static void Process(Assembly assemblyDefinition, NameTranslationInfo nameTranslationInfo)
    {
        var ctx = new NameTranslationApplierContext(nameTranslationInfo);
        ctx.Process(assemblyDefinition);
    }

    private string TranslateName(string name) =>
        _nameTranslationInfo.NameTranslation.GetValueOrDefault(name, name);

    private bool TryTranslateName(string name, out string translatedName)
    {
        if (_nameTranslationInfo.NameTranslation.TryGetValue(name, out var translated))
        {
            translatedName = translated;
            return true;
        }

        translatedName = null;
        return false;
    }

    private string TranslateNamespace(string className, string currentNamespace) =>
        _nameTranslationInfo.ClassNamespaces.GetValueOrDefault(className, currentNamespace);

    private void Process(Assembly assemblyDefinition)
    {
        foreach (var module in assemblyDefinition.DefinedTypes)
            Process(module);
    }

    private void Process(TypeInfo typeDefinition)
    {
        if (TryTranslateName(typeDefinition.Name, out var translatedName))
        {
            typeDefinition.Namespace = TranslateNamespace(typeDefinition.Name, typeDefinition.Namespace);
            typeDefinition.Name = translatedName;
        }

        foreach (var method in typeDefinition.DeclaredMethods)
            Process(method);

        foreach (var field in typeDefinition.DeclaredFields)
            Process(field);

        foreach (var property in typeDefinition.DeclaredProperties)
            Process(property);

        foreach (var evt in typeDefinition.DeclaredEvents)
            Process(evt);

        foreach (var nestedType in typeDefinition.DeclaredNestedTypes)
            Process(nestedType);
    }

    private void Process(MethodInfo methodDefinition)
    {
        methodDefinition.Name = TranslateName(methodDefinition.Name);

        foreach (var parameter in methodDefinition.DeclaredParameters)
            Process(parameter);
    }

    private void Process(FieldInfo fieldDefinition)
    {
        fieldDefinition.Name = TranslateName(fieldDefinition.Name);
    }

    private void Process(PropertyInfo propertyDefinition)
    {
        propertyDefinition.Name = TranslateName(propertyDefinition.Name);
    }

    private void Process(EventInfo eventDefinition)
    {
        eventDefinition.Name = TranslateName(eventDefinition.Name);
    }

    private void Process(ParameterInfo parameterDefinition)
    {
        parameterDefinition.Name = TranslateName(parameterDefinition.Name);
    }
}
#nullable enable
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Cpp;

// This follows Itanium/GCC mangling specifications.
public class MangledNameBuilder
{
    private readonly StringBuilder _sb = new("_Z");

    public override string ToString()
        => _sb.ToString();

    public static string Method(MethodBase method)
    {
        var builder = new MangledNameBuilder();
        builder.BuildMethod(method);
        return builder.ToString();
    }

    public static string MethodInfo(MethodBase method)
    {
        var builder = new MangledNameBuilder();
        builder.BuildMethod(method, "MethodInfo");
        return builder.ToString();
    }

    public static string TypeInfo(TypeInfo type)
    {
        var builder = new MangledNameBuilder();
        builder.BeginName();
        builder.WriteIdentifier("TypeInfo");
        builder.WriteTypeName(type);
        builder.WriteEnd();
        return builder.ToString();
    }

    public static string TypeRef(TypeInfo type)
    {
        var builder = new MangledNameBuilder();
        builder.BeginName();
        builder.WriteIdentifier("TypeRef");
        builder.WriteTypeName(type);
        builder.WriteEnd();
        return builder.ToString();
    }

    private void BuildMethod(MethodBase method, string prefix = "")
    {
        /*
         * We do not have any CV-qualifiers nor ref-qualifiers,
         * so we immediately write the nested name.
         */

        BeginName();

        if (prefix.Length > 0)
            WriteIdentifier(prefix);

        WriteTypeName(method.DeclaringType);

        switch (method.Name)
        {
            case ".ctor":
                _sb.Append("C1"); // Constructor
                break;
            case ".cctor":
                WriteIdentifier("cctor");
                break;
            default:
                WriteIdentifier(method.Name);
                break;
        }

        var genericParams = method.GetGenericArguments();

        WriteGenericParams(genericParams);

        WriteEnd(); // End nested name

        // Now write the method parameters

        if (genericParams.Length > 0 && method is MethodInfo mInfo)
        {
            // If this is a generic method, the first parameter needs to be the return type
            WriteType(mInfo.ReturnType);
        }

        if (method.DeclaredParameters.Count == 0)
            _sb.Append('v');
        else
        {
            foreach (var param in method.DeclaredParameters)
                WriteType(param.ParameterType);
        }
    }

    private void WriteTypeName(TypeInfo type)
    {
        if (type.HasElementType)
            type = type.ElementType;

        WriteName(type.Namespace);

        if (type.DeclaringType != null)
            WriteIdentifier(type.DeclaringType.Name);

        WriteIdentifier(type.CSharpBaseName);
        WriteGenericParams(type.GenericTypeArguments);
    }

    private void WriteType(TypeInfo type)
    {
        if (type.FullName == "System.Void")
        {
            _sb.Append('v');
            return;
        }

        if (type.IsByRef)
            _sb.Append('R');

        if (type.IsPointer)
            _sb.Append('P');

        if (type.IsArray)
            _sb.Append("A_");

        if (type.IsPrimitive && type.Name != "Decimal")
        {
            if (type.Name is "IntPtr" or "UIntPtr")
                _sb.Append("Pv"); // void*
            else
            {
                _sb.Append(type.Name switch
                {
                    "Boolean" => 'b',
                    "Byte" => 'h',
                    "SByte" => 'a',
                    "Int16" => 's',
                    "UInt16" => 't',
                    "Int32" => 'i',
                    "UInt32" => 'j',
                    "Int64" => 'l',
                    "UInt64" => 'm',
                    "Char" => 'w',
                    "Single" => 'f',
                    "Double" => 'd',
                    _ => throw new UnreachableException()
                });
            }
        }
        else
        {
            BeginName();
            WriteTypeName(type);
            WriteEnd();
        }
    }

    private void WriteGenericParams(TypeInfo[] generics)
    {
        if (generics.Length > 0)
        {
            BeginGenerics();

            foreach (var arg in generics)
                WriteType(arg);

            WriteEnd();
        }
    }

    private void WriteIdentifier(string identifier)
    {
        identifier = MangledRegex.Gcc.Replace(identifier, "_");

        _sb.Append(identifier.Length);
        _sb.Append(identifier);
    }

    private void WriteName(string name)
    {
        foreach (var part in name.Split("."))
        {
            if (part.Length > 0)
                WriteIdentifier(part);
        }
    }

    private void BeginName()
    {
        _sb.Append('N');
    }

    private void BeginGenerics()
    {
        _sb.Append('I');
    }

    private void WriteEnd()
    {
        _sb.Append('E');
    }
}

internal static partial class MangledRegex
{
    public static Regex Gcc
    {
        get
        {
            _gcc ??= GccNameRegex();
            return _gcc;
        }
    }

    private static Regex? _gcc;

    [GeneratedRegex("[^a-zA-Z0-9_]")]
    public static partial Regex GccNameRegex();
}
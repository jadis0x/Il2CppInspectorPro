#nullable enable
using System.Collections;
using System.Diagnostics;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Cpp;

public class CppTypeDependencyGraph
{
    private sealed class CppTypeNode(TypeInfo typeInfo)
    {
        public TypeInfo Type { get; } = typeInfo;
        public HashSet<CppTypeNode> IncomingValueReferences { get; } = [];
        public HashSet<CppTypeNode> OutgoingValueReferences { get; } = [];
        public HashSet<CppTypeNode> IncomingReferenceReferences { get; } = [];
        public HashSet<CppTypeNode> OutgoingReferenceReferences { get; } = [];

        public override string ToString()
            => Type.ToString();

    }

    private readonly Dictionary<TypeInfo, CppTypeNode> _typeNodes = [];
    private readonly HashSet<CppTypeNode> _alreadyProcessedNodes = [];
    private readonly Queue<CppTypeNode> _currentlyProcessingNodes = [];

    private void AddReference(CppTypeNode from, CppTypeNode to, bool isReference)
    {
        // We do not count references to itself, even though they could occur
        // as they cannot be value references
        if (from.Type == to.Type)
            return;

        // if the target node is already processed, skip adding the reference
        // as it is as assumed to already be present in any output.
        if (_alreadyProcessedNodes.Contains(to))
            return;

        if (isReference)
        {
            from.OutgoingReferenceReferences.Add(to);
            to.IncomingReferenceReferences.Add(from);
        }
        else
        {
            from.OutgoingValueReferences.Add(to);
            to.IncomingValueReferences.Add(from);
        }
    }

    private CppTypeNode GetNode(TypeInfo typeInfo)
    {
        if (_typeNodes.TryGetValue(typeInfo, out var typeNode))
            return typeNode;

        _typeNodes[typeInfo] = typeNode = new CppTypeNode(typeInfo);
        CollectReferences(typeNode);
        _currentlyProcessingNodes.Enqueue(typeNode);

        return typeNode;
    }

    private void CollectReferences(CppTypeNode typeNode)
    {
        var typeInfo = typeNode.Type;

        // if we have a base type, reference it.
        // this is always a value reference
        if (typeInfo.BaseType != null)
            AddReference(typeNode, GetNode(typeInfo.BaseType), false);

        // if this is an array, we also reference the element type.
        // the reference type depends on the type of the element
        if (typeInfo.IsArray)
            AddReference(typeNode, GetNode(typeInfo.ElementType), typeInfo.ElementType.IsPassedByReference);

        // if we are an enum, reference the underlying type.
        // this is always a value reference
        if (typeInfo.IsEnum)
            AddReference(typeNode, GetNode(typeInfo.GetEnumUnderlyingType()), false);

        // finally, process all instance fields.
        // the reference type depends on the type of the field
        foreach (var field in typeInfo.DeclaredFields.Where(f => f.IsInstanceField))
            AddReference(typeNode, GetNode(field.FieldType), field.FieldType.IsPassedByReference);
    }

    public List<TypeInfo> DeriveDependencyOrderedTypes(TypeInfo typeInfo)
    {
        _currentlyProcessingNodes.Clear();

        // We assume that all nodes that are *not* in _currentlyProcessingNodes have already been
        // processed in a previous call to DeriveDependencyOrderedTypes.

        // initialize all dependencies for the given type info
        _ = GetNode(typeInfo);

        var dependencyOrderedTypes = new List<TypeInfo>(_currentlyProcessingNodes.Count);

        while (_currentlyProcessingNodes.Count > 0)
        {
            var remainingCount = _currentlyProcessingNodes.Count;

            for (int i = 0; i < remainingCount; i++)
            {
                var node = _currentlyProcessingNodes.Dequeue();

                // If this node still has outgoing value references (=> non-ref references to unprocessed types), we cannot emit it yet.
                if (node.OutgoingValueReferences.Count > 0)
                {
                    _currentlyProcessingNodes.Enqueue(node);
                    continue;
                }

                // otherwise, we can safely emit it now.
                dependencyOrderedTypes.Add(node.Type);

                // now that we have emitted this node, we can remove it from the references of all other unprocessed nodes.
                foreach (var referencingNode in node.IncomingValueReferences)
                    referencingNode.OutgoingValueReferences.Remove(node);

                foreach (var referencingNode in node.IncomingReferenceReferences)
                    referencingNode.OutgoingReferenceReferences.Remove(node);

                // ...clear all references from this node as well
                node.IncomingValueReferences.Clear();
                node.IncomingReferenceReferences.Clear();

                // and mark it as processed
                _alreadyProcessedNodes.Add(node);
            }

            // if we have not processed any node in this iteration, we have a circular dependency of some value types.
            if (_currentlyProcessingNodes.Count == remainingCount)
                throw new InvalidOperationException("Failed to resolve circular dependency during C++ type ordering.");
        }

        return dependencyOrderedTypes;
    }

    public void Reset()
    {
        _typeNodes.Clear();
        _currentlyProcessingNodes.Clear();
    }
}

// L-TODO: Do we want to expose these helper extensions on the types themselves?
file static class Extensions
{
    extension(TypeInfo typeInfo)
    {
        public bool IsPassedByReference =>
            typeInfo.IsByRef || typeInfo.IsPointer || !typeInfo.IsValueType;
    }

    extension(FieldInfo fieldInfo)
    {
        public bool IsInstanceField => fieldInfo is { IsLiteral: false, IsStatic: false, IsThreadStatic: false };
    }
}
﻿using AssemblyRemapper.Enums;
using AssemblyRemapper.Models;
using AssemblyRemapper.Utils;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AssemblyRemapper.Remapper.Search;

internal static class TypeDefExtensions
{
    public static EMatchResult MatchIsAbstract(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.IsAbstract is null)
        {
            return EMatchResult.Disabled;
        }

        // Interfaces cannot be abstract, and abstract cannot be static
        if (type.IsInterface || type.GetStaticConstructor() is not null)
        {
            return EMatchResult.NoMatch;
        }

        if (type.IsAbstract == parms.IsAbstract)
        {
            score.Score++;
            return EMatchResult.Match;
        }

        score.FailureReason = EFailureReason.IsAbstract;
        return EMatchResult.NoMatch;
    }

    public static EMatchResult MatchIsEnum(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.IsEnum is null)
        {
            return EMatchResult.Disabled;
        }

        if (type.IsEnum == parms.IsEnum)
        {
            score.Score++;
            return EMatchResult.Match;
        }

        score.FailureReason = EFailureReason.IsEnum;
        return EMatchResult.NoMatch;
    }

    public static EMatchResult MatchIsNested(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.IsNested is null)
        {
            return EMatchResult.Disabled;
        }

        if (type.IsNested == parms.IsNested)
        {
            score.Score++;
            return EMatchResult.Match;
        }

        score.FailureReason = EFailureReason.IsNested;
        return EMatchResult.NoMatch;
    }

    public static EMatchResult MatchIsSealed(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.IsSealed is null)
        {
            return EMatchResult.Disabled;
        }

        if (type.IsSealed == parms.IsSealed)
        {
            score.Score++;
            return EMatchResult.Match;
        }

        score.FailureReason = EFailureReason.IsSealed;
        return EMatchResult.NoMatch;
    }

    public static EMatchResult MatchIsDerived(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.IsDerived is null)
        {
            return EMatchResult.Disabled;
        }

        if (type.BaseType is not null && (bool)parms.IsDerived is true)
        {
            score.Score++;
            return EMatchResult.Match;
        }

        if (type.BaseType?.Name == parms.MatchBaseClass)
        {
            return EMatchResult.Match;
        }

        if (type.BaseType?.Name == parms.IgnoreBaseClass)
        {
            return EMatchResult.NoMatch;
        }

        score.FailureReason = EFailureReason.IsDerived;
        return EMatchResult.NoMatch;
    }

    public static EMatchResult MatchIsInterface(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.IsInterface is null)
        {
            return EMatchResult.Disabled;
        }

        if (type.IsInterface == parms.IsInterface)
        {
            score.Score++;
            return EMatchResult.Match;
        }

        score.FailureReason = EFailureReason.IsInterface;
        return EMatchResult.NoMatch;
    }

    public static EMatchResult MatchHasGenericParameters(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.HasGenericParameters is null)
        {
            return EMatchResult.Disabled;
        }

        if (type.HasGenericParameters == parms.HasGenericParameters)
        {
            score.Score++;
            return EMatchResult.Match;
        }

        score.FailureReason = EFailureReason.HasGenericParameters;
        return EMatchResult.NoMatch;
    }

    public static EMatchResult MatchIsPublic(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.IsPublic is null)
        {
            return EMatchResult.Disabled;
        }

        if (parms.IsPublic is false && type.IsNotPublic is true)
        {
            score.Score++;
            return EMatchResult.Match;
        }
        else if (parms.IsPublic is true && type.IsPublic is true)
        {
            score.Score++;
            return EMatchResult.Match;
        }

        score.FailureReason = EFailureReason.IsPublic;
        return EMatchResult.NoMatch;
    }

    public static EMatchResult MatchHasAttribute(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.HasAttribute is null)
        {
            return EMatchResult.Disabled;
        }

        if (type.HasCustomAttributes == parms.HasAttribute)
        {
            score.Score++;
            return EMatchResult.Match;
        }

        score.FailureReason = EFailureReason.HasAttribute;
        return EMatchResult.NoMatch;
    }

    public static EMatchResult MatchConstructors(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        var matches = new List<EMatchResult> { };

        if (parms.ConstructorParameterCount is not null)
        {
            matches.Add(Constructors.GetTypeByParameterCount(type, parms, score));
        }

        return matches.GetMatch();
    }

    /// <summary>
    /// Handle running all method matching routines
    /// </summary>
    /// <returns>Match if any search criteria met</returns>
    public static EMatchResult MatchMethods(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        var matches = new List<EMatchResult> { };

        if (parms.MatchMethods.Count > 0 && parms.IgnoreMethods.Contains("*") is true)
        {
            Logger.Log($"Cannot both ignore all methods and search for a method on {score.ProposedNewName}.", ConsoleColor.Red);
            return EMatchResult.NoMatch;
        }
        else if (parms.MatchMethods.Count > 0)
        {
            matches.Add(Methods.GetTypeWithMethods(type, parms, score));
        }

        if (parms.IgnoreMethods.Count > 0)
        {
            Logger.Log("TypeWithoutMethods");
            matches.Add(Methods.GetTypeWithoutMethods(type, parms, score));
        }

        if (parms.IgnoreMethods.Contains("*"))
        {
            Logger.Log("TypeWithNoMethods");
            matches.Add(Methods.GetTypeWithNoMethods(type, parms, score));
        }

        if (parms.MethodCount > 0)
        {
            Logger.Log("TypeByNumberOfMethods");
            matches.Add(Methods.GetTypeByNumberOfMethods(type, parms, score));
        }

        // return match if any condition matched
        return matches.GetMatch();
    }

    public static EMatchResult MatchFields(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.MatchFields.Count is 0 && parms.IgnoreFields.Count is 0)
        {
            return EMatchResult.Disabled;
        }

        var skippAll = parms.IgnoreFields.Contains("*");

        // Type has fields, we dont want any
        if (type.HasFields is false && skippAll is true)
        {
            score.Score++;
            return EMatchResult.Match;
        }

        int matchCount = 0;

        foreach (var field in type.Fields)
        {
            if (parms.IgnoreFields.Contains(field.Name))
            {
                // Type contains blacklisted field
                score.FailureReason = EFailureReason.HasFields;
                return EMatchResult.NoMatch;
            }
        }

        foreach (var field in type.Fields)
        {
            if (parms.MatchFields.Contains(field.Name))
            {
                matchCount++;
                score.Score++;
            }
        }

        return matchCount > 0 ? EMatchResult.Match : EMatchResult.NoMatch;
    }

    public static EMatchResult MatchProperties(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.MatchProperties.Count is 0 && parms.IgnorePropterties.Count is 0)
        {
            return EMatchResult.Disabled;
        }

        var skippAll = parms.IgnorePropterties.Contains("*");

        // Type has fields, we dont want any
        if (type.HasProperties is false && skippAll is true)
        {
            return EMatchResult.Match;
        }

        foreach (var property in type.Properties)
        {
            if (parms.IgnorePropterties.Contains(property.Name))
            {
                // Type contains blacklisted property
                score.FailureReason = EFailureReason.HasProperties;
                return EMatchResult.NoMatch;
            }
        }

        int matchCount = 0;

        foreach (var property in type.Properties)
        {
            if (parms.MatchProperties.Contains(property.Name))
            {
                matchCount++;
                score.Score++;
            }
        }

        return matchCount > 0 ? EMatchResult.Match : EMatchResult.NoMatch;
    }

    public static EMatchResult MatchNestedTypes(this TypeDefinition type, SearchParams parms, ScoringModel score)
    {
        if (parms.MatchNestedTypes.Count is 0 && parms.IgnoreNestedTypes.Count is 0)
        {
            return EMatchResult.Disabled;
        }

        var skippAll = parms.IgnorePropterties.Contains("*");

        // `*` is the wildcard to ignore all fields that exist on types
        if (type.HasNestedTypes is false && skippAll is true)
        {
            score.FailureReason = EFailureReason.HasNestedTypes;
            return EMatchResult.Match;
        }

        foreach (var nestedType in type.NestedTypes)
        {
            if (parms.IgnoreNestedTypes.Contains(nestedType.Name))
            {
                // Type contains blacklisted nested type
                score.FailureReason = EFailureReason.HasNestedTypes;
                return EMatchResult.NoMatch;
            }
        }

        int matchCount = 0;

        foreach (var nestedType in type.NestedTypes)
        {
            if (parms.MatchNestedTypes.Contains(nestedType.Name))
            {
                matchCount++;
                score.Score++;
            }
        }

        return matchCount > 0 ? EMatchResult.Match : EMatchResult.NoMatch;
    }
}
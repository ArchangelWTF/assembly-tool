﻿using AssemblyRemapper.Utils;

namespace AssemblyRemapper.Reflection;

internal static class Publicizer
{
    public static void Publicize()
    {
        if (!DataProvider.AppSettings.Publicize) { return; }

        Logger.Log("Starting publicization...", ConsoleColor.Green);

        foreach (var type in DataProvider.ModuleDefinition.Types)
        {
            if (type.IsNotPublic) { type.IsPublic = true; }

            // We only want to do methods and properties

            if (type.HasMethods)
            {
                foreach (var method in type.Methods)
                {
                    method.IsPublic = true;
                }
            }

            if (type.HasProperties)
            {
                foreach (var property in type.Properties)
                {
                    if (property.SetMethod != null)
                    {
                        property.SetMethod.IsPublic = true;
                    }

                    if (property.GetMethod != null)
                    {
                        property.GetMethod.IsPublic = true;
                    }
                }
            }
        }
    }

    public static void Unseal()
    {
        if (!DataProvider.AppSettings.Unseal) { return; }

        Logger.Log("Starting unseal...", ConsoleColor.Green);

        foreach (var type in DataProvider.ModuleDefinition.Types)
        {
            if (type.IsSealed) { type.IsSealed = false; }
        }
    }
}
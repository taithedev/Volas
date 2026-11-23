using System;
using BepInEx;
using UnityEngine;

namespace Volas
{
    // This file lives in src/ to keep a dedicated diagnostics/compatibility area.
    // It is included by Volas.csproj and compiled into the same Volas.dll.
    internal static class AssemblyDiagnostics
    {
        internal static string AssemblyInfo()
        {
            try
            {
                var unity = typeof(MonoBehaviour).Assembly.GetName();
                var loader = typeof(BaseUnityPlugin).Assembly.GetName();
                return "Runtime assemblies: " + unity.Name + " (" + unity.Version + "), " +
                       loader.Name + " (" + loader.Version + ")";
            }
            catch (Exception error)
            {
                return "Unable to inspect Unity assemblies: " + error.Message;
            }
        }
    }
}

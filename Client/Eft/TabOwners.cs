using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace ImprovedTabUI.Eft
{
    internal static class TabOwners
    {
        private const int MinimumKeyLength = 4;

        private static readonly FieldInfo ControllerField = AccessTools.Field(typeof(Tab), "Controller");
        private static readonly Dictionary<Assembly, string> AssemblyOwners = new Dictionary<Assembly, string>();

        private static List<PluginInfo> _plugins;

        internal static bool HasController(Tab tab)
        {
            return tab != null && ControllerField != null && ControllerField.GetValue(tab) != null;
        }

        internal static void CollectShared(Tab tab, HashSet<string> shared)
        {
            foreach (string owner in OwnersOf(tab))
            {
                shared.Add(owner);
            }
        }

        internal static string Resolve(Tab tab, HashSet<string> shared)
        {
            foreach (string owner in OwnersOf(tab))
            {
                if (!shared.Contains(owner))
                {
                    return owner;
                }
            }

            return tab != null ? OwnerByName(tab.gameObject.name) : null;
        }

        private static List<string> OwnersOf(Tab tab)
        {
            List<string> owners = new List<string>();

            if (tab == null)
            {
                return owners;
            }

            try
            {
                Add(owners, tab.GetType());

                object controller = ControllerField != null ? ControllerField.GetValue(tab) : null;
                Add(owners, controller != null ? controller.GetType() : null);

                foreach (MonoBehaviour behaviour in tab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    Add(owners, behaviour != null ? behaviour.GetType() : null);
                }
            }
            catch (Exception)
            {
            }

            return owners;
        }

        private static void Add(List<string> owners, Type type)
        {
            if (type == null)
            {
                return;
            }

            if (!AssemblyOwners.TryGetValue(type.Assembly, out string owner))
            {
                owner = FindOwner(type.Assembly);
                AssemblyOwners[type.Assembly] = owner;
            }

            if (owner != null && !owners.Contains(owner))
            {
                owners.Add(owner);
            }
        }

        private static string FindOwner(Assembly assembly)
        {
            if (assembly == typeof(TabOwners).Assembly || assembly.IsDynamic)
            {
                return null;
            }

            string location = FullPath(assembly.Location);
            string root = FullPath(BepInEx.Paths.PluginPath);

            foreach (PluginInfo plugin in Plugins())
            {
                bool sameAssembly = plugin.Instance != null && plugin.Instance.GetType().Assembly == assembly;

                if (sameAssembly || (location != null && Same(location, FullPath(plugin.Location))))
                {
                    return NameOf(plugin);
                }
            }

            if (location == null)
            {
                return null;
            }

            foreach (PluginInfo plugin in Plugins())
            {
                string pluginPath = FullPath(plugin.Location);
                string folder = pluginPath != null ? Path.GetDirectoryName(pluginPath) : null;

                if (folder != null && !Same(folder, root) && location.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return NameOf(plugin);
                }
            }

            return null;
        }

        private static string OwnerByName(string objectName)
        {
            string name = Normalize(objectName);
            string best = null;
            int bestLength = MinimumKeyLength - 1;

            foreach (PluginInfo plugin in Plugins())
            {
                string guid = plugin.Metadata.GUID ?? string.Empty;
                string pluginPath = FullPath(plugin.Location);
                string[] keys =
                {
                    Normalize(NameOf(plugin)),
                    Normalize(guid.Substring(guid.LastIndexOf('.') + 1)),
                    Normalize(pluginPath != null ? Path.GetFileNameWithoutExtension(pluginPath) : null)
                };

                foreach (string key in keys)
                {
                    if (key.Length > bestLength && name.Contains(key))
                    {
                        best = NameOf(plugin);
                        bestLength = key.Length;
                    }
                }
            }

            return best;
        }

        private static List<PluginInfo> Plugins()
        {
            if (_plugins == null)
            {
                _plugins = Chainloader.PluginInfos.Values
                    .Where(plugin => plugin != null && plugin.Metadata != null && plugin.Metadata.GUID != Plugin.PluginGuid)
                    .ToList();
            }

            return _plugins;
        }

        private static string NameOf(PluginInfo plugin)
        {
            return string.IsNullOrWhiteSpace(plugin.Metadata.Name) ? plugin.Metadata.GUID : plugin.Metadata.Name.Trim();
        }

        private static string Normalize(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : new string(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }

        private static bool Same(string first, string second)
        {
            return second != null && string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }

        private static string FullPath(string path)
        {
            try
            {
                return string.IsNullOrEmpty(path) ? null : Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

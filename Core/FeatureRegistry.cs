using System;
using System.Collections.Generic;

namespace Volas
{
    internal sealed class Feature
    {
        #region Feature metadata

        internal readonly string Id;
        internal readonly string Category;
        internal readonly string Label;
        internal readonly string Description;
        internal readonly bool ActionOnly;
        internal readonly bool NeedsModdedRoom;
        internal readonly Action<VolasPlugin> OnEnable;
        internal readonly Action<VolasPlugin> OnDisable;
        internal readonly Action<VolasPlugin> OnUpdate;
        internal readonly Action<VolasPlugin> OnFixedUpdate;
        internal bool Enabled;

        #endregion

        #region Construction

        internal Feature(string id, string category, string label, string description,
            Action<VolasPlugin> onEnable, Action<VolasPlugin> onDisable = null,
            Action<VolasPlugin> onUpdate = null, Action<VolasPlugin> onFixedUpdate = null,
            bool actionOnly = false, bool needsModdedRoom = false)
        {
            Id = id;
            Category = category;
            Label = label;
            Description = description;
            OnEnable = onEnable;
            OnDisable = onDisable;
            OnUpdate = onUpdate;
            OnFixedUpdate = onFixedUpdate;
            ActionOnly = actionOnly;
            NeedsModdedRoom = needsModdedRoom;
        }

        #endregion
    }

    internal sealed class FeatureRegistry
    {
        #region Registry state

        private readonly VolasPlugin plugin;
        internal readonly List<Feature> All = new List<Feature>();
        internal readonly List<string> Categories = new List<string>();
        private readonly HashSet<string> favorites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal int Revision { get; private set; }
        internal string LastMessage { get; private set; } = "Ready";
        internal int FaultCount { get; private set; }
        internal string LastFault { get; private set; } = "No recorded faults";
        internal string CatalogStatus { get; private set; } = "Catalog not checked";
        internal int ActiveCount { get { int count = 0; foreach (var feature in All) if (feature.Enabled) count++; return count; } }

        internal FeatureRegistry(VolasPlugin owner) { plugin = owner; }

        #endregion

        #region Registration & discovery

        internal void Add(Feature f)
        {
            if (Find(f.Id) != null) throw new InvalidOperationException("Duplicate Volas feature: " + f.Id);
            All.Add(f);
            if (Categories.Count == 0) { Categories.Add("All"); Categories.Add("Favorites"); }
            if (!Categories.Contains(f.Category)) Categories.Add(f.Category);
            Revision++;
        }

        internal void ValidateCatalog()
        {
            int errors = 0;
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var feature in All)
            {
                if (string.IsNullOrWhiteSpace(feature.Id) || !ids.Add(feature.Id) || feature.OnEnable == null) errors++;
            }
            CatalogStatus = errors == 0 ? "Catalog check: " + All.Count + " registered handlers OK" : "Catalog check: " + errors + " invalid registrations";
        }

        internal Feature Find(string id)
        {
            return All.Find(f => f.Id == id);
        }

        internal bool IsOn(string id)
        {
            var f = Find(id);
            return f != null && f.Enabled;
        }

        internal List<Feature> InCategory(string category)
        {
            if (category == "All") return new List<Feature>(All);
            return category == "Favorites"
                ? All.FindAll(f => favorites.Contains(f.Id))
                : All.FindAll(f => f.Category == category);
        }

        #endregion

        #region Favorites

        internal bool IsFavorite(Feature feature)
        {
            return feature != null && favorites.Contains(feature.Id);
        }

        internal void LoadFavorites(string serialized)
        {
            favorites.Clear();
            foreach (string id in (serialized ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string cleaned = id.Trim();
                if (Find(cleaned) != null) favorites.Add(cleaned);
            }
            Revision++;
        }

        internal void ToggleFavorite(Feature feature)
        {
            if (feature == null) return;
            if (!favorites.Add(feature.Id)) favorites.Remove(feature.Id);
            var saved = new List<string>(favorites);
            saved.Sort(StringComparer.OrdinalIgnoreCase);
            plugin.Settings.FavoriteIds.Value = string.Join(",", saved.ToArray());
            LastMessage = feature.Label + (IsFavorite(feature) ? " added to favorites" : " removed from favorites");
            Revision++;
        }

        #endregion

        #region Activation & error handling

        internal void Activate(Feature f)
        {
            if (f == null) return;
            if (f.Enabled)
            {
                Disable(f);
                return;
            }
            if (f.NeedsModdedRoom && !plugin.Game.IsSoloOrModdedRoom())
            {
                LastMessage = f.Label + ": solo or MODDED room only";
                plugin.Log.LogWarning(LastMessage);
                Revision++;
                return;
            }

            if (f.ActionOnly)
            {
                try { f.OnEnable?.Invoke(plugin); LastMessage = f.Label + ": executed"; }
                catch (Exception ex) { Fault(f, ex); }
                Revision++;
                return;
            }

            try
            {
                f.OnEnable?.Invoke(plugin);
                f.Enabled = true;
                LastMessage = f.Label + " enabled";
            }
            catch (Exception ex)
            {
                try { f.OnDisable?.Invoke(plugin); } catch { }
                f.Enabled = false;
                Fault(f, ex);
            }
            Revision++;
        }

        internal void Disable(Feature f)
        {
            if (f == null || !f.Enabled) return;
            f.Enabled = false;
            try { f.OnDisable?.Invoke(plugin); }
            catch (Exception ex) { Fault(f, ex); Revision++; return; }
            LastMessage = f.Label + " disabled";
            Revision++;
        }

        private void Fault(Feature f, Exception ex)
        {
            FaultCount++;
            LastFault = f.Label + ": " + ex.GetBaseException().Message;
            LastMessage = LastFault;
            plugin.Log.LogError("Volas feature " + f.Id + " failed: " + ex);
        }

        #endregion

        #region Runtime updates & reset

        internal void Tick()
        {
            bool? roomAllowed = null;
            foreach (var f in All)
            {
                if (!f.Enabled) continue;
                if (f.NeedsModdedRoom && !(roomAllowed ?? (roomAllowed = plugin.Game.IsSoloOrModdedRoom()).Value))
                {
                    Disable(f);
                    continue;
                }
                try { f.OnUpdate?.Invoke(plugin); }
                catch (Exception ex) { Fault(f, ex); Disable(f); }
            }
        }

        internal void FixedTick()
        {
            bool? roomAllowed = null;
            foreach (var f in All)
            {
                if (!f.Enabled) continue;
                if (f.NeedsModdedRoom && !(roomAllowed ?? (roomAllowed = plugin.Game.IsSoloOrModdedRoom()).Value))
                {
                    Disable(f);
                    continue;
                }
                try { f.OnFixedUpdate?.Invoke(plugin); }
                catch (Exception ex) { Fault(f, ex); Disable(f); }
            }
        }

        internal void DisableAll()
        {
            foreach (var f in All)
                Disable(f);
            if (plugin.Guns != null)
                plugin.Guns.Reset();
            if (plugin.CreativeGun != null)
                plugin.CreativeGun.Reset();
            if (plugin.Decorations != null)
                plugin.Decorations.Reset();
            if (plugin.Charms != null) plugin.Charms.Dispose();
            if (plugin.Gun != null)
                plugin.Gun.Reset();
            if (plugin.ExtraVisuals != null)
                plugin.ExtraVisuals.Reset();
        }

        #endregion
    }
}

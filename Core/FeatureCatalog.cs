namespace Volas
{
    // Composition root: feature definitions live with their respective systems.
    internal static class FeatureCatalog
    {
        internal static void Register(VolasPlugin plugin)
        {
            var registry = plugin.Features;
            MovementFeatures.Register(registry);
            VisualFeatures.Register(registry);
            TelemetryFeatures.Register(registry);
            UtilityFeatures.Register(registry);
            SettingsFeatures.Register(registry);
        }
    }
}

using Godot;
using ManagementGame.Application;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace ManagementGame.SponsorWorkspace;

public partial class SponsorVerification
{
    // Same native workflow for both managed configurations. All waits are outside
    // discrete work timers; the separate settled cycle explicitly includes them.
    private async Task Performance(string output)
    {
        var assembly = typeof(SponsorWorkspaceView).Assembly;
        var assemblyPath = ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/Client.dll");
        var configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        if (configuration != Arg("--sponsor-configuration")) throw new InvalidOperationException("Wrong loaded assembly configuration");
        await Setup(); await Open();
        var coldBind = View.Timings["bind"][0];
        var replacedDocuments = 0;
        var sameTheme = true;
        var profile = new Dictionary<string, List<double>>();
        var performanceSamples = new Dictionary<string, List<double>>();
        for (var i = -5; i < 50; i++)
        {
            await Setup(); await Open();
            if (i >= 0) Add("initial-bind-work", View.Timings["bind"][0]);
            var controls = Descendants(View).OfType<Control>().Count();
            if (i == 0) retainedBefore = controls;
            var theme = View.Theme;
            var document = View.TermsScroll.GetInstanceId();
            // Fresh observation, including a real revision change: not a no-op bind.
            session.Submit(new("perf:revision", 0, new CoachDecision(Delegation.Manual, Risk.Balanced)));
            View.Presenter.Refresh();
            var start = Stopwatch.GetTimestamp(); View.Render(); Record("ordinary-rebind-work", start);
            if (i >= 0 && View.TermsScroll.GetInstanceId() != document) replacedDocuments++;
            sameTheme &= theme == View.Theme;
            await Settle(1);
            document = View.TermsScroll.GetInstanceId();
            start = Stopwatch.GetTimestamp(); View.Act("review"); Record("review-confirmation-work", start);
            if (i >= 0 && View.TermsScroll.GetInstanceId() != document) replacedDocuments++;
            await Settle(1);
            start = Stopwatch.GetTimestamp(); View.Act("back"); Record("confirmation-review-work", start);
            await Settle(1);
            View.Act("review"); await Settle(1);
            start = Stopwatch.GetTimestamp(); View.Act("commit"); Record("commit-through-render-work", start);
            if (View.Presenter.Phase != SponsorPhase.Accepted) throw new InvalidOperationException("Performance command rejected");
            if (i >= 0)
            {
                foreach (var pair in View.Presenter.Timings) Add(pair.Key, pair.Value.Single());
                foreach (var pair in View.Timings.Where(x => x.Key.StartsWith("profile-", StringComparison.Ordinal)))
                {
                    if (!profile.TryGetValue(pair.Key, out var values)) profile[pair.Key] = values = [];
                    values.AddRange(pair.Value);
                }
            }
            await Settle(1);
            start = Stopwatch.GetTimestamp(); View.Act("back"); Record("return-work", start);
            await Settle(); await Setup();
            var cycle = Stopwatch.GetTimestamp();
            start = Stopwatch.GetTimestamp(); Matter().EmitSignal(BaseButton.SignalName.Pressed); Entry().EmitSignal(BaseButton.SignalName.Pressed); Record("enter-work", start);
            await Settle();
            start = Stopwatch.GetTimestamp(); View.Act("back"); Record("back-work", start);
            await Settle(); Record("enter-back-plus-layout", cycle);
            await Open(); retainedAfter = Descendants(View).OfType<Control>().Count();
            if (retainedAfter != controls) throw new InvalidOperationException("Retained Control growth");

            void Record(string key, long tick) { if (i >= 0) Add(key, Stopwatch.GetElapsedTime(tick).TotalMilliseconds); }
        }
        object Summary(List<double> values)
        {
            var sorted = values.Order().ToArray();
            return new { Count = sorted.Length, Median = sorted[(sorted.Length - 1) / 2], P95 = sorted[(int)Math.Ceiling(sorted.Length * .95) - 1], Max = sorted[^1] };
        }
        var result = new
        {
            Configuration = configuration, JitOptimizerDisabled = assembly.GetCustomAttribute<DebuggableAttribute>()!.IsJITOptimizerDisabled,
            Assembly = assemblyPath, LoadedModuleId = assembly.ManifestModule.ModuleVersionId,
            AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assemblyPath))),
            Commit = Arg("--sponsor-commit"), SourceDigest = Arg("--sponsor-source-digest"), Utc = DateTime.UtcNow,
            Engine = Engine.GetVersionInfo()["string"].AsString(), Viewport = GetViewportRect().Size.ToString(),
            ColdInitialBind = coldBind, Warmup = 5, Iterations = 50, RetainedControls = new { Before = retainedBefore, After = retainedAfter },
            ReplacedDocumentRegions = replacedDocuments, SameTheme = sameTheme,
            Summary = performanceSamples.ToDictionary(x => x.Key, x => Summary(x.Value)), Samples = performanceSamples,
            Profile = profile.ToDictionary(x => x.Key, x => Summary(x.Value)),
            Limits = "Local native editor engine, optimized managed Release vs managed Debug; not an exported release engine, GPU frame budget, physical latency, or QG-02 qualification. Layout cycle includes fixed frame waits."
        };
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "performance.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print("SPONSOR_PERFORMANCE_PASS " + configuration);
        void Add(string key, double value) { if (!performanceSamples.TryGetValue(key, out var values)) performanceSamples[key] = values = []; values.Add(value); }
    }
}

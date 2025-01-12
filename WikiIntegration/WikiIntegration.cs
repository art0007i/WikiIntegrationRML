using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Linq;
using FrooxEngine;
using Elements.Core;
using FrooxEngine.ProtoFlux;
using FrooxEngine.UIX;
using ProtoFlux.Core;

namespace WikiIntegration;

public class WikiIntegration : ResoniteMod
{
    public const string VERSION = "0.1.0";
    public override string Name => "WikiIntegration";
    public override string Author => "Banane9 & art0007i";
    public override string Version => VERSION;
    public override string Link => "https://github.com/art0007i/WikiIntegrationRML/";

    private static ModConfiguration config;
    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> _components = new("Components", "Whether to add the Wiki button to Components in Worker Inspectors.", () => true);
    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> _protoFlux = new("ProtoFlux", "Whether to add the Wiki button to ProtoFlux nodes.", () => true);
    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<int> _componentOffset = new("ComponentOffset", "The Order Offset of the Wiki button on Inspector Headers. Range: -16 to 16 - Higher is further right.", () => 2, false, (i) => i.IsBetween(-16, 16));

    private static readonly string ComponentLocale = "Opens the Resonite Wiki article about this Component.";
    private static readonly string ProtoFluxLocale = "Opens the Resonite Wiki article about this ProtoFlux node.";
    public override void OnEngineInit()
    {
        config = GetConfiguration();
        Harmony harmony = new Harmony("me.art0007i.WikiIntegration");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(WorkerInspector))]
    [HarmonyPatch("BuildUIForComponent")]
    internal sealed class WikiButtonPatch
    {
        private static void Postfix(Worker worker, WorkerInspector __instance)
        {
            if (!config.GetValue(_components)) return;
            if (worker is Slot) return;

            var ui = new UIBuilder(__instance.Slot.Children.Last().Children.First());

            RadiantUI_Constants.SetupEditorStyle(ui);
            ui.Style.FlexibleWidth = 0f;
            ui.Style.MinWidth = 40f;

            var button = ui.Button(OfficialAssets.Graphics.Badges.Mentor);
            AddTooltip(button.Slot, worker is ProtoFluxNode ? ProtoFluxLocale : ComponentLocale);

            AddHyperlink(button.Slot, worker);
            button.Slot.OrderOffset = config.GetValue(_componentOffset);
        }
    }


    [HarmonyPatch(typeof(ProtoFluxNodeVisual), nameof(ProtoFluxNodeVisual.GenerateVisual))]
    internal sealed class OpenWikiArticleButton
    {

        private static void Postfix(ProtoFluxNodeVisual __instance, ProtoFluxNode node)
        {
            if (!config.GetValue(_protoFlux)) return;
            var ui = new UIBuilder(__instance.LocalUIBuilder.Canvas);

            var buttonArea = ui.Panel();
            ui.IgnoreLayout();
            buttonArea.AnchorMin.Value = new(1, 0);
            buttonArea.AnchorMax.Value = new(1, 0);
            buttonArea.OffsetMin.Value = new(-12, 2);
            buttonArea.OffsetMax.Value = new(-2, 12);

            var button = ui.Image(OfficialAssets.Graphics.Badges.Mentor);
            button.Slot.AttachComponent<Button>();
            AddTooltip(button.Slot, ProtoFluxLocale);

            AddHyperlink(button.Slot, node);
        }
    }

    private static void AddHyperlink(Slot slot, Worker worker)
    {
        string wikiPage;
        LocaleString reason;

        if (worker is ProtoFluxNode node)
        {
            reason = ProtoFluxLocale;
            var nodeName = node.NodeName;

            var nodeMetadata = NodeMetadataHelper.GetMetadata(node.NodeType);
            if (!string.IsNullOrEmpty(nodeMetadata.Overload))
            {
                var overload = nodeMetadata.Overload;
                var dotIndex = overload.LastIndexOf('.');

                nodeName = dotIndex > 0 ? overload.Substring(dotIndex + 1) : nodeName;
            }

            wikiPage = $"ProtoFlux:{nodeName.Replace(' ', '_')}";
        }
        else
        {
            reason = ComponentLocale;
            var workerName = worker.WorkerType.Name;

            // Don't need to remove the `1 on generics - they redirect
            wikiPage = $"Component:{workerName}";
        }

        var hyperlink = slot.AttachComponent<Hyperlink>();
        hyperlink.URL.Value = new Uri($"https://wiki.resonite.com/{wikiPage}");
        hyperlink.Reason.AssignLocaleString(reason);
    }

    private static void AddTooltip(Slot slot, string tip)
    {
        var comment = slot.AttachComponent<Comment>();
        comment.Text.Value = "TooltipperyLabel:" + tip;
    }
}

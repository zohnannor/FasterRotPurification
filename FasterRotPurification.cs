using System.Security.Permissions;
using BepInEx;
using Menu.Remix.MixedUI;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace FasterRotPurification;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class FasterRotPurificationMain : BaseUnityPlugin {
    public const string PLUGIN_GUID = "zohnannor.fasterrotpurification";
    public const string PLUGIN_NAME = "Faster Rot Purification";
    public const string PLUGIN_VERSION = "1.0.0";

    private bool initDone = false;
    public static FasterRotPurificationOptions Options;

    public void OnEnable() {
        On.RainWorld.OnModsInit += OnModsInit;
    }

    public void OnDisable() {
        On.RainWorld.OnModsInit -= OnModsInit;
        On.Room.UpdateSentientRotEffect -= Room_UpdateSentientRotEffect;
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self) {
        orig(self);
        if (initDone) {
            return;
        }

        Options = new FasterRotPurificationOptions();
        MachineConnector.SetRegisteredOI(PLUGIN_GUID, Options);

        On.Room.UpdateSentientRotEffect += Room_UpdateSentientRotEffect;

        Logger.LogDebug($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded");
        initDone = true;
    }

    private void Room_UpdateSentientRotEffect(On.Room.orig_UpdateSentientRotEffect orig, Room self) {
        AdjustPurificationIntensity(self);
        orig(self);
    }

    private void AdjustPurificationIntensity(Room self) {
        if (
            self.game == null
                || !self.game.IsStorySession
                || self.world?.regionState == null
                || self.abstractRoom == null
                || !self.didFirstSentientRotUpdate
                || !self.game.GetStorySession.saveState.miscWorldSaveData.hasVoidWeaverAbility
        ) {
            return;
        }

        var region = self.world.regionState;
        if (!region.sentientRotProgression.TryGetValue(self.abstractRoom.name, out var prog)) {
            return;
        }

        float m = Options.Multiplier.Value;
        if (Mathf.Approximately(m, 1f)) {
            return;
        }
        float perTick = 0.005f * (m - 1f) / 8f;

        prog.rotIntensity = Mathf.Clamp(prog.rotIntensity - perTick, 0f, 1f);
    }
}

public class FasterRotPurificationOptions : OptionInterface {
    public readonly Configurable<float> Multiplier;

    private OpTab mainTab;
    private OpUpdown _multiplier;

    private const string desc = "Multiplier for the Void Weaver ability rot purification effect. 1.0 = vanilla, 0.5 = twice as long, 10.0 = ten times faster.";

    public FasterRotPurificationOptions() {
        Multiplier = config.Bind(
            "multiplier",
            1f,
            new ConfigurableInfo(
                desc,
                new ConfigAcceptableRange<float>(0.5f, 100f)
            )
        );
    }

    public override void Initialize() {
        base.Initialize();

        mainTab = new(this, "Main");
        Tabs = [mainTab];

        _multiplier = new(Multiplier, new(75f, 524f), 100f) {
            description = desc
        };

        mainTab.AddItems([
            _multiplier,
            new OpLabel(5f, 530f, "Multiplier") {
                alignment = FLabelAlignment.Left,
                description = desc
            },
        ]);
    }
}

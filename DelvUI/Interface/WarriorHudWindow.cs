using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Plugin;
using DelvUI.Interface.Bars;
using ImGuiNET;

namespace DelvUI.Interface
{
    public class WarriorHudWindow : HudWindow
    {
        public override uint JobId => 21;

        private int StormsEyeHeight => PluginConfiguration.WARStormsEyeHeight;
        private int StormsEyeWidth => PluginConfiguration.WARStormsEyeWidth;

        private new int XOffset => PluginConfiguration.WARBaseXOffset;
        private new int YOffset => PluginConfiguration.WARBaseYOffset;

        private int BeastGaugeHeight => PluginConfiguration.WARBeastGaugeHeight;
        private int BeastGaugeWidth => PluginConfiguration.WARBeastGaugeWidth;
        private int BeastGaugePadding => PluginConfiguration.WARBeastGaugePadding;
        private int BeastGaugeXOffset => PluginConfiguration.WARBeastGaugeXOffset;
        private int BeastGaugeYOffset => PluginConfiguration.WARBeastGaugeYOffset;

        private int InterBarOffset => PluginConfiguration.WARInterBarOffset;

        private Dictionary<string, uint> InnerReleaseColor => PluginConfiguration.JobColorMap[Jobs.WAR * 1000];
        private Dictionary<string, uint> StormsEyeColor => PluginConfiguration.JobColorMap[Jobs.WAR * 1000 + 1];
        private Dictionary<string, uint> FellCleaveColor => PluginConfiguration.JobColorMap[Jobs.WAR * 1000 + 2];
        private Dictionary<string, uint> NascentChaosColor => PluginConfiguration.JobColorMap[Jobs.WAR * 1000 + 3];
        private Dictionary<string, uint> EmptyColor => PluginConfiguration.JobColorMap[Jobs.WAR * 1000 + 4];

        public WarriorHudWindow(
            ClientState clientState,
            DalamudPluginInterface pluginInterface,
            DataManager dataManager,
            Framework framework,
            GameGui gameGui,
            JobGauges jobGauges,
            ObjectTable objectTable, 
            PluginConfiguration pluginConfiguration,
            TargetManager targetManager,
            UiBuilder uiBuilder
        ) : base(
            clientState,
            pluginInterface,
            dataManager,
            framework,
            gameGui,
            jobGauges,
            objectTable,
            pluginConfiguration,
            targetManager,
            uiBuilder
        ) { }

        protected override void Draw(bool _) {
            var nextHeight = DrawStormsEyeBar(0);
            DrawBeastGauge(nextHeight);
        }

        protected override void DrawPrimaryResourceBar() {
        }

        private int DrawStormsEyeBar(int initialHeight) {
            Debug.Assert(ClientState.LocalPlayer != null, "ClientState.LocalPlayer != null");
            var innerReleaseBuff = ClientState.LocalPlayer.StatusList.Where(o => o.StatusId == 1177);
            var stormsEyeBuff = ClientState.LocalPlayer.StatusList.Where(o => o.StatusId == 90);

            var xPos = CenterX - XOffset;
            var yPos = CenterY + YOffset + initialHeight;

            var builder = BarBuilder.Create(xPos, yPos, StormsEyeHeight, StormsEyeWidth);

            var duration = 0f;
            var maximum = 10f;
            var color = EmptyColor;

            if (innerReleaseBuff.Any()) {
                duration = Math.Abs(innerReleaseBuff.First().RemainingTime);
                color = InnerReleaseColor;
            }
            else if (stormsEyeBuff.Any()) {
                duration = Math.Abs(stormsEyeBuff.First().RemainingTime);
                maximum = 60f;
                color = StormsEyeColor;
            }

            var bar = builder.AddInnerBar(duration, maximum, color)
                .SetTextMode(BarTextMode.EachChunk)
                .SetText(BarTextPosition.CenterMiddle, BarTextType.Current)
                .Build();
            
            var drawList = ImGui.GetWindowDrawList();
            bar.Draw(drawList);

            return StormsEyeHeight + initialHeight + InterBarOffset;
        }

        private int DrawBeastGauge(int initialHeight) {
            Debug.Assert(ClientState.LocalPlayer != null, "ClientState.LocalPlayer != null");
            var gauge = JobGauges.Get<WARGauge>();
            var nascentChaosBuff = ClientState.LocalPlayer.StatusList.Where(o => o.StatusId == 1897);
            
            var xPos = CenterX - XOffset + BeastGaugeXOffset;
            var yPos = CenterY + YOffset + initialHeight + BeastGaugeYOffset;

            var builder = BarBuilder.Create(xPos, yPos, BeastGaugeHeight, BeastGaugeWidth)
                .SetChunks(2)
                .AddInnerBar(gauge.BeastGauge, 100, FellCleaveColor, EmptyColor)
                .SetChunkPadding(BeastGaugePadding);

            if (nascentChaosBuff.Any()) {
                builder.SetChunksColors(NascentChaosColor);
            }

            var bar = builder.Build();

            var drawList = ImGui.GetWindowDrawList();
            bar.Draw(drawList);

            return BeastGaugeHeight + initialHeight + InterBarOffset;
        }
    }
}
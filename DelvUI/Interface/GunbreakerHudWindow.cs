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

namespace DelvUI.Interface {
    public class GunbreakerHudWindow : HudWindow {
        public override uint JobId => 37;

        private new int XOffset => PluginConfiguration.GNBBaseXOffset;
        private new int YOffset => PluginConfiguration.GNBBaseYOffset;

        private bool PowderGaugeEnabled => PluginConfiguration.GNBPowderGaugeEnabled;
        private int PowderGaugeHeight => PluginConfiguration.GNBPowderGaugeHeight;
        private int PowderGaugeWidth => PluginConfiguration.GNBPowderGaugeWidth;
        private int PowderGaugeXOffset => PluginConfiguration.GNBPowderGaugeXOffset;
        private int PowderGaugeYOffset => PluginConfiguration.GNBPowderGaugeYOffset;
        private int PowderGaugePadding => PluginConfiguration.GNBPowderGaugePadding;
        private Dictionary<string, uint> GunPowderColor => PluginConfiguration.JobColorMap[Jobs.GNB * 1000];

        private bool NoMercyBarEnabled => PluginConfiguration.GNBNoMercyBarEnabled;
        private int NoMercyBarHeight => PluginConfiguration.GNBNoMercyBarHeight;
        private int NoMercyBarWidth => PluginConfiguration.GNBNoMercyBarWidth;
        private int NoMercyBarXOffset => PluginConfiguration.GNBNoMercyBarXOffset;
        private int NoMercyBarYOffset => PluginConfiguration.GNBNoMercyBarYOffset;
        private Dictionary<string, uint> NoMercyColor => PluginConfiguration.JobColorMap[Jobs.GNB * 1000 + 1];

        private int InterBarOffset => PluginConfiguration.GNBInterBarOffset;

        public GunbreakerHudWindow(
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
            var initialOffset = YOffset;

            if (PowderGaugeEnabled) {
                initialOffset = DrawPowderGauge(initialOffset);
            }

            if (NoMercyBarEnabled) {
                DrawNoMercyBar(initialOffset);
            }
        }
        
        protected override void DrawPrimaryResourceBar() {
        }

        private int DrawPowderGauge(int initialOffset) {
            var gauge = JobGauges.Get<GNBGauge>();

            var xPos = CenterX - XOffset + PowderGaugeXOffset;
            var yPos = CenterY + initialOffset + PowderGaugeYOffset;

            var builder = BarBuilder.Create(xPos, yPos, PowderGaugeHeight, PowderGaugeWidth);
            builder.SetChunks(2)
                .SetChunkPadding(PowderGaugePadding)
                .AddInnerBar(gauge.Ammo, 2, GunPowderColor, null);

            var drawList = ImGui.GetWindowDrawList();
            builder.Build().Draw(drawList);

            return initialOffset + PowderGaugeHeight + InterBarOffset;
        }

        private void DrawNoMercyBar(int initialOffset) {
            var xPos = CenterX - XOffset + NoMercyBarXOffset;
            var yPos = CenterY + initialOffset + NoMercyBarYOffset;

            Debug.Assert(ClientState.LocalPlayer != null, "ClientState.LocalPlayer != null");
            var noMercyBuff = ClientState.LocalPlayer.StatusList.Where(o => o.StatusId == 1831);

            var builder = BarBuilder.Create(xPos, yPos, NoMercyBarHeight, NoMercyBarWidth);

            if (noMercyBuff.Any())
            {
                var duration = noMercyBuff.First().RemainingTime;
                builder.AddInnerBar(duration, 20, NoMercyColor, null)
                    .SetTextMode(BarTextMode.EachChunk)
                    .SetText(BarTextPosition.CenterMiddle, BarTextType.Current);
            }

            var drawList = ImGui.GetWindowDrawList();
            builder.Build().Draw(drawList);
        }
    }
}
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;

namespace DelvUI.Interface
{
    public class DragoonHudWindow : HudWindow
    {
        public override uint JobId => Jobs.DRG;

        private int EyeOfTheDragonBarHeight => PluginConfiguration.DRGEyeOfTheDragonHeight;
        private int EyeOfTheDragonBarWidth => PluginConfiguration.DRGEyeOfTheDragonBarWidth;
        private int EyeOfTheDragonPadding => PluginConfiguration.DRGEyeOfTheDragonPadding;
        private new int XOffset => PluginConfiguration.DRGBaseXOffset;
        private new int YOffset => PluginConfiguration.DRGBaseYOffset;
        private int BloodBarHeight => PluginConfiguration.DRGBloodBarHeight;
        private int DisembowelBarHeight => PluginConfiguration.DRGDisembowelBarHeight;
        private int ChaosThrustBarHeight => PluginConfiguration.DRGChaosThrustBarHeight;
        private int InterBarOffset => PluginConfiguration.DRGInterBarOffset;
        private bool ShowChaosThrustTimer => PluginConfiguration.DRGShowChaosThrustTimer;
        private bool ShowDisembowelTimer => PluginConfiguration.DRGShowDisembowelBuffTimer;
        private bool ShowChaosThrustText => PluginConfiguration.DRGShowChaosThrustText;
        private bool ShowBloodText => PluginConfiguration.DRGShowBloodText;
        private bool ShowDisembowelText => PluginConfiguration.DRGShowDisembowelText;
        private Dictionary<string, uint> EyeOfTheDragonColor => PluginConfiguration.JobColorMap[Jobs.DRG * 1000];
        private Dictionary<string, uint> BloodOfTheDragonColor => PluginConfiguration.JobColorMap[Jobs.DRG * 1000 + 1];
        private Dictionary<string, uint> LifeOfTheDragonColor => PluginConfiguration.JobColorMap[Jobs.DRG * 1000 + 2];
        private Dictionary<string, uint> DisembowelColor => PluginConfiguration.JobColorMap[Jobs.DRG * 1000 + 3];
        private Dictionary<string, uint> ChaosThrustColor => PluginConfiguration.JobColorMap[Jobs.DRG * 1000 + 4];
        private Dictionary<string, uint> EmptyColor => PluginConfiguration.JobColorMap[Jobs.DRG * 1000 + 5];

        public DragoonHudWindow(
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

        protected override void Draw(bool _)
        {
            var nextHeight = 0;
            
            if (ShowChaosThrustTimer) {
                nextHeight = DrawChaosThrustBar(nextHeight);
            }
            
            if (ShowDisembowelTimer) {
                nextHeight = DrawDisembowelBar(nextHeight);
            }
            
            nextHeight = DrawEyeOfTheDragonBars(nextHeight);
            DrawBloodOfTheDragonBar(nextHeight);
        }

        protected override void DrawPrimaryResourceBar()
        {
            // Never draw the mana bar for Dragoons as it's useless.
            return;
        }

        private int DrawChaosThrustBar(int initialHeight)
        {
            Debug.Assert(ClientState.LocalPlayer != null, "ClientState.LocalPlayer != null");
            var actor = TargetManager.SoftTarget ?? TargetManager.Target;
            var scale = 0f;
            var duration = 0;
            if (actor is BattleChara target) {
                var chaosThrust = target.StatusList.FirstOrDefault(o => o.StatusId is 1312 or 118 && o.SourceID == ClientState.LocalPlayer.ObjectId);
                scale = chaosThrust?.RemainingTime ?? 0f / 24f;
                duration = (int) Math.Round(chaosThrust?.RemainingTime ?? 0f);
                if (scale < 0f) {
                    scale = 0f;
                    duration = 0;
                }
            }
            var barWidth = EyeOfTheDragonBarWidth * 2 + EyeOfTheDragonPadding;
            var barSize = new Vector2(barWidth, ChaosThrustBarHeight);
            var xPos = CenterX - XOffset;
            var yPos = CenterY + YOffset + initialHeight;
            var cursorPos = new Vector2(xPos, yPos);
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(cursorPos, cursorPos + barSize, EmptyColor["background"]);
            drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
            var chaosThrustBarSize = new Vector2(barWidth * scale, ChaosThrustBarHeight);
            
            drawList.AddRectFilledMultiColor(
                cursorPos, cursorPos + chaosThrustBarSize,
                ChaosThrustColor["gradientLeft"], ChaosThrustColor["gradientRight"], ChaosThrustColor["gradientRight"], ChaosThrustColor["gradientLeft"]);

            if (ShowChaosThrustText && duration > 0f) {
                var durationText = duration.ToString();
                var textSize = ImGui.CalcTextSize(durationText);
                DrawOutlinedText(duration.ToString(), new Vector2(cursorPos.X + 5f, cursorPos.Y + ChaosThrustBarHeight / 2f - textSize.Y / 2f));
            }

            return initialHeight + ChaosThrustBarHeight + InterBarOffset;
        }

        private int DrawEyeOfTheDragonBars(int initialHeight)
        {
            var gauge = JobGauges.Get<DRGGauge>();

            var barSize = new Vector2(EyeOfTheDragonBarWidth, EyeOfTheDragonBarHeight);
            var xPos = CenterX - XOffset;
            var yPos = CenterY + YOffset + initialHeight;
            var cursorPos = new Vector2(xPos, yPos);
            var eyeCount = gauge.EyeCount;
            var drawList = ImGui.GetWindowDrawList();

            for (byte i = 0; i < 2; i++) {
                cursorPos = new Vector2(cursorPos.X + (EyeOfTheDragonBarWidth + EyeOfTheDragonPadding) * i, cursorPos.Y);
                if (eyeCount >= i + 1)
                {
                    drawList.AddRectFilledMultiColor(
                        cursorPos, cursorPos + barSize,
                        EyeOfTheDragonColor["gradientLeft"], EyeOfTheDragonColor["gradientRight"], EyeOfTheDragonColor["gradientRight"], EyeOfTheDragonColor["gradientLeft"]
                    );
                }
                else {
                    drawList.AddRectFilled(cursorPos, cursorPos + barSize, EmptyColor["background"]);
                }
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
            }

            return EyeOfTheDragonBarHeight + initialHeight + InterBarOffset;
        }

        private int DrawBloodOfTheDragonBar(int initialHeight)
        {
            var gauge = JobGauges.Get<DRGGauge>();

            var xPos = CenterX - XOffset;
            var yPos = CenterY + YOffset + initialHeight;
            var barWidth = EyeOfTheDragonBarWidth * 2 + EyeOfTheDragonPadding;
            var cursorPos = new Vector2(xPos, yPos);
            var barSize = new Vector2(barWidth, BloodBarHeight);

            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(cursorPos, cursorPos + barSize, EmptyColor["background"]);
            drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);

            const int maxTimerMs = 30 * 1000;
            var currTimerMs = gauge.BOTDTimer;
            if (currTimerMs == 0)
            {
                return initialHeight + BloodBarHeight + InterBarOffset;
            }
            var scale = (float)currTimerMs / maxTimerMs;
            var botdBarSize = new Vector2(barWidth * scale, BloodBarHeight);
            if (gauge.BOTDState == BOTDState.LOTD)
            {
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + botdBarSize,
                    LifeOfTheDragonColor["gradientLeft"], LifeOfTheDragonColor["gradientRight"], LifeOfTheDragonColor["gradientRight"], LifeOfTheDragonColor["gradientLeft"]);
            }
            else {
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + botdBarSize,
                    BloodOfTheDragonColor["gradientLeft"], BloodOfTheDragonColor["gradientRight"], BloodOfTheDragonColor["gradientRight"], BloodOfTheDragonColor["gradientLeft"]);
            }
            
            if (ShowBloodText) {
                var durationText = ((int)(currTimerMs / 1000f)).ToString();
                var textSize = ImGui.CalcTextSize(durationText);
                DrawOutlinedText(durationText, new Vector2(cursorPos.X + 5f, cursorPos.Y + BloodBarHeight / 2f - textSize.Y / 2f));
            }
            
            return initialHeight + BloodBarHeight + InterBarOffset;
        }

        private int DrawDisembowelBar(int initialHeight)
        {
            Debug.Assert(ClientState.LocalPlayer != null, "ClientState.LocalPlayer != null");
            var xPos = CenterX - XOffset;
            var yPos = CenterY + YOffset + initialHeight;
            var barWidth = EyeOfTheDragonBarWidth * 2 + EyeOfTheDragonPadding;
            var barSize = new Vector2(barWidth, DisembowelBarHeight);
            var cursorPos = new Vector2(xPos, yPos);
            var drawList = ImGui.GetWindowDrawList();
            var disembowelBuff = ClientState.LocalPlayer.StatusList.Where(o => o.StatusId is 1914 or 121);
            drawList.AddRectFilled(cursorPos, cursorPos + barSize, EmptyColor["background"]);
            drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
            if (!disembowelBuff.Any())
            {
                return initialHeight + DisembowelBarHeight + InterBarOffset;
            }
            
            var buff = disembowelBuff.First();
            if (buff.RemainingTime <= 0)
            {
                return initialHeight + DisembowelBarHeight + InterBarOffset;
            }
            var scale = buff.RemainingTime / 30f;
            var disembowelBarSize = new Vector2(barWidth * scale, DisembowelBarHeight);
            drawList.AddRectFilledMultiColor(
                cursorPos, cursorPos + disembowelBarSize,
                DisembowelColor["gradientLeft"], DisembowelColor["gradientRight"], DisembowelColor["gradientRight"], DisembowelColor["gradientLeft"]);

            if (ShowDisembowelText)
            {
                var durationText = ((int)buff.RemainingTime).ToString();
                var textSize = ImGui.CalcTextSize(durationText);
                DrawOutlinedText(durationText, new Vector2(cursorPos.X + 5f, cursorPos.Y + BloodBarHeight / 2f - textSize.Y / 2f));
            }

            return initialHeight + DisembowelBarHeight + InterBarOffset;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Plugin;
using DelvUI.Enums;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using DelvUI.Helpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using BattleChara = FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara;
using Character = Dalamud.Game.ClientState.Objects.Types.Character;

namespace DelvUI.Interface {
    public abstract class HudWindow {
        public bool IsVisible = true;
        protected readonly ClientState ClientState;
        protected readonly DataManager DataManager;
        protected readonly Framework Framework;
        protected readonly GameGui GameGui;
        protected readonly JobGauges JobGauges;
        protected readonly ObjectTable ObjectTable;
        protected readonly PluginConfiguration PluginConfiguration;
        protected readonly DalamudPluginInterface PluginInterface;
        protected readonly TargetManager TargetManager;
        protected readonly UiBuilder UiBuilder;

        public abstract uint JobId { get; }

        protected static float CenterX => ImGui.GetMainViewport().Size.X / 2f;
        protected static float CenterY => ImGui.GetMainViewport().Size.Y / 2f;
        protected static int XOffset => 160;
        protected static int YOffset => 460;

        protected int HealthBarHeight => PluginConfiguration.HealthBarHeight;
        protected int HealthBarWidth => PluginConfiguration.HealthBarWidth;
        protected int HealthBarXOffset => PluginConfiguration.HealthBarXOffset;
        protected int HealthBarYOffset => PluginConfiguration.HealthBarYOffset;
        protected int HealthBarTextLeftXOffset => PluginConfiguration.HealthBarTextLeftXOffset;
        protected int HealthBarTextLeftYOffset => PluginConfiguration.HealthBarTextLeftYOffset;
        protected int HealthBarTextRightXOffset => PluginConfiguration.HealthBarTextRightXOffset;
        protected int HealthBarTextRightYOffset => PluginConfiguration.HealthBarTextRightYOffset;

        protected int PrimaryResourceBarHeight => PluginConfiguration.PrimaryResourceBarHeight;
        protected int PrimaryResourceBarWidth => PluginConfiguration.PrimaryResourceBarWidth;
        protected int PrimaryResourceBarXOffset => PluginConfiguration.PrimaryResourceBarXOffset;
        protected int PrimaryResourceBarYOffset => PluginConfiguration.PrimaryResourceBarYOffset;

        protected int TargetBarHeight => PluginConfiguration.TargetBarHeight;
        protected int TargetBarWidth => PluginConfiguration.TargetBarWidth;
        protected int TargetBarXOffset => PluginConfiguration.TargetBarXOffset;
        protected int TargetBarYOffset => PluginConfiguration.TargetBarYOffset;
        protected int TargetBarTextLeftXOffset => PluginConfiguration.TargetBarTextLeftXOffset;
        protected int TargetBarTextLeftYOffset => PluginConfiguration.TargetBarTextLeftYOffset;
        protected int TargetBarTextRightXOffset => PluginConfiguration.TargetBarTextRightXOffset;
        protected int TargetBarTextRightYOffset => PluginConfiguration.TargetBarTextRightYOffset;

        protected int ToTBarHeight => PluginConfiguration.ToTBarHeight;
        protected int ToTBarWidth => PluginConfiguration.ToTBarWidth;
        protected int ToTBarXOffset => PluginConfiguration.ToTBarXOffset;
        protected int ToTBarYOffset => PluginConfiguration.ToTBarYOffset;
        protected int ToTBarTextXOffset => PluginConfiguration.ToTBarTextXOffset;
        protected int ToTBarTextYOffset => PluginConfiguration.ToTBarTextYOffset;

        protected int FocusBarHeight => PluginConfiguration.FocusBarHeight;
        protected int FocusBarWidth => PluginConfiguration.FocusBarWidth;
        protected int FocusBarXOffset => PluginConfiguration.FocusBarXOffset;
        protected int FocusBarYOffset => PluginConfiguration.FocusBarYOffset;
        protected int FocusBarTextXOffset => PluginConfiguration.FocusBarTextXOffset;
        protected int FocusBarTextYOffset => PluginConfiguration.FocusBarTextYOffset;

        protected int MPTickerHeight => PluginConfiguration.MPTickerHeight;
        protected int MPTickerWidth => PluginConfiguration.MPTickerWidth;
        protected int MPTickerXOffset => PluginConfiguration.MPTickerXOffset;
        protected int MPTickerYOffset => PluginConfiguration.MPTickerYOffset;
        protected bool MPTickerShowBorder => PluginConfiguration.MPTickerShowBorder;
        protected bool MPTickerHideOnFullMp => PluginConfiguration.MPTickerHideOnFullMp;

        protected int GCDIndicatorHeight => PluginConfiguration.GCDIndicatorHeight;
        protected int GCDIndicatorWidth => PluginConfiguration.GCDIndicatorWidth;
        protected int GCDIndicatorXOffset => PluginConfiguration.GCDIndicatorXOffset;
        protected int GCDIndicatorYOffset => PluginConfiguration.GCDIndicatorYOffset;
        protected bool GCDIndicatorShowBorder => PluginConfiguration.GCDIndicatorShowBorder;

        protected int CastBarWidth => PluginConfiguration.CastBarWidth;
        protected int CastBarHeight => PluginConfiguration.CastBarHeight;
        protected int CastBarXOffset => PluginConfiguration.CastBarXOffset;
        protected int CastBarYOffset => PluginConfiguration.CastBarYOffset;
        
        protected int TargetCastBarWidth => PluginConfiguration.TargetCastBarWidth;
        protected int TargetCastBarHeight => PluginConfiguration.TargetCastBarHeight;
        protected int TargetCastBarXOffset => PluginConfiguration.TargetCastBarXOffset;
        protected int TargetCastBarYOffset => PluginConfiguration.TargetCastBarYOffset;

        protected Vector2 BarSize { get; private set; }

        private LastUsedCast _lastPlayerUsedCast;
        private LastUsedCast _lastTargetUsedCast;

        // private delegate void OpenContextMenuFromTarget(IntPtr agentHud, IntPtr gameObject);
        // private OpenContextMenuFromTarget openContextMenuFromTarget;
        
        private MpTickHelper _mpTickHelper;

        protected HudWindow(
            ClientState clientState,
            DalamudPluginInterface pluginInterface,
            DataManager dataManager,
            Framework framework,
            GameGui gameGui,
            JobGauges jobGauges,
            ObjectTable objectTable, 
            PluginConfiguration pluginConfiguration,
            TargetManager targetManager,
            UiBuilder uiBuilder) 
        {
            ClientState = clientState;
            DataManager = dataManager;
            Framework = framework;
            GameGui = gameGui;
            JobGauges = jobGauges;
            ObjectTable = objectTable;
            PluginConfiguration = pluginConfiguration;
            PluginInterface = pluginInterface;
            TargetManager = targetManager;
            UiBuilder = uiBuilder;

            // openContextMenuFromTarget = Marshal.GetDelegateForFunctionPointer<OpenContextMenuFromTarget>(PluginInterface.TargetModuleScanner.ScanText("48 85 D2 74 7F 48 89 5C 24"));
            PluginConfiguration.ConfigChangedEvent += OnConfigChanged;
        }

        protected void OnConfigChanged(object sender, EventArgs args)
        {
            if (!PluginConfiguration.MPTickerEnabled) {
                _mpTickHelper = null;
            } 
        }

        protected virtual void DrawHealthBar() {
            Debug.Assert(ClientState.LocalPlayer != null, "ClientState.LocalPlayer != null");
            BarSize = new Vector2(HealthBarWidth, HealthBarHeight);
            var actor = ClientState.LocalPlayer;
            var scale = (float) actor.CurrentHp / actor.MaxHp;

            if (PluginConfiguration.TankStanceIndicatorEnabled && (actor.ClassJob.Id is 19 or 32 or 21 or 37)) {
                DrawTankStanceIndicator();
            }

            var cursorPos = new Vector2(CenterX - HealthBarWidth - HealthBarXOffset, CenterY + HealthBarYOffset);
            ImGui.SetCursorPos(cursorPos);

            PluginConfiguration.JobColorMap.TryGetValue(ClientState.LocalPlayer.ClassJob.Id, out var colors);
            colors ??= PluginConfiguration.NPCColorMap["friendly"];

            if (PluginConfiguration.CustomHealthBarColorEnabled) colors = PluginConfiguration.MiscColorMap["customhealth"];

            var drawList = ImGui.GetWindowDrawList();

            // Basically make an invisible box for BeginChild to work properly.
            ImGuiWindowFlags windowFlags = 0;
            windowFlags |= ImGuiWindowFlags.NoBackground;
            windowFlags |= ImGuiWindowFlags.NoTitleBar;
            windowFlags |= ImGuiWindowFlags.NoMove;
            windowFlags |= ImGuiWindowFlags.NoDecoration;

            ImGui.SetNextWindowPos(cursorPos);
            ImGui.SetNextWindowSize(BarSize);

            ImGui.Begin("health_bar", windowFlags);
            if (ImGui.BeginChild("health_bar", BarSize)) {
                drawList.AddRectFilled(cursorPos, cursorPos + BarSize, colors["background"]);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(HealthBarWidth * scale, HealthBarHeight),
                    colors["gradientLeft"], colors["gradientRight"], colors["gradientRight"], colors["gradientLeft"]
                );
                drawList.AddRect(cursorPos, cursorPos + BarSize, 0xFF000000);

                // Check if mouse is hovering over the box properly
                if (ImGui.GetIO().MouseClicked[0] && ImGui.IsMouseHoveringRect(cursorPos, cursorPos + BarSize)) {
                    TargetManager.SetTarget(actor);
                }
            }

            ImGui.EndChild();
            ImGui.End();

            DrawTargetShield(actor, cursorPos, BarSize, true);

            DrawOutlinedText(
                $"{TextTags.GenerateFormattedTextFromTags(actor, PluginConfiguration.HealthBarTextLeft)}",
                new Vector2(cursorPos.X + 5 + HealthBarTextLeftXOffset, cursorPos.Y - 22 + HealthBarTextLeftYOffset)
            );

            var text = TextTags.GenerateFormattedTextFromTags(actor, PluginConfiguration.HealthBarTextRight);
            var textSize = ImGui.CalcTextSize(text);

            DrawOutlinedText(text,
                new Vector2(cursorPos.X + HealthBarWidth - textSize.X - 5 + HealthBarTextRightXOffset,
                    cursorPos.Y - 22 + HealthBarTextRightYOffset
                )
            );
        }

        protected virtual void DrawPrimaryResourceBar() {
            Debug.Assert(ClientState.LocalPlayer != null, "ClientState.LocalPlayer != null");
            var actor = ClientState.LocalPlayer;
            var scale = (float) actor.CurrentMp / actor.MaxMp;
            BarSize = new Vector2(PrimaryResourceBarWidth, PrimaryResourceBarHeight);
            var cursorPos = new Vector2(CenterX - PrimaryResourceBarXOffset + 33, CenterY + PrimaryResourceBarYOffset - 16);

            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(cursorPos, cursorPos + BarSize, 0x88000000);
            drawList.AddRectFilledMultiColor(
                cursorPos, cursorPos + new Vector2(BarSize.X * scale, BarSize.Y),
                0xFFE6CD00, 0xFFD8Df3C, 0xFFD8Df3C, 0xFFE6CD00
            );
            drawList.AddRect(cursorPos, cursorPos + BarSize, 0xFF000000);
        }

        protected virtual void DrawTargetBar() {
            var target = TargetManager.SoftTarget ?? TargetManager.Target;

            if (target is null) {
                return;
            }

            BarSize = new Vector2(TargetBarWidth, TargetBarHeight);

            var cursorPos = new Vector2(CenterX + TargetBarXOffset, CenterY + TargetBarYOffset);
            ImGui.SetCursorPos(cursorPos);

            var drawList = ImGui.GetWindowDrawList();
            if (target is not Character actor) {
                var friendly = PluginConfiguration.NPCColorMap["friendly"];
                drawList.AddRectFilled(cursorPos, cursorPos + BarSize, friendly["background"]);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(TargetBarWidth, TargetBarHeight),
                    friendly["gradientLeft"], friendly["gradientRight"],
                    friendly["gradientRight"], friendly["gradientLeft"]
                );
                drawList.AddRect(cursorPos, cursorPos + BarSize, 0xFF000000);
            }
            else {
                var scale = actor.MaxHp > 0f ? (float) actor.CurrentHp / actor.MaxHp : 0f;
                var colors = DetermineTargetPlateColors(actor);
                drawList.AddRectFilled(cursorPos, cursorPos + BarSize, colors["background"]);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(TargetBarWidth * scale, TargetBarHeight),
                    colors["gradientLeft"], colors["gradientRight"], colors["gradientRight"], colors["gradientLeft"]
                );
                drawList.AddRect(cursorPos, cursorPos + BarSize, 0xFF000000);

                DrawTargetShield(target, cursorPos, BarSize, true);
            }
            
            var textLeft = TextTags.GenerateFormattedTextFromTags(target, PluginConfiguration.TargetBarTextLeft);
            DrawOutlinedText(textLeft,
                new Vector2(cursorPos.X + 5 + TargetBarTextLeftXOffset,
                    cursorPos.Y - 22 + TargetBarTextLeftYOffset));

            var textRight = TextTags.GenerateFormattedTextFromTags(target, PluginConfiguration.TargetBarTextRight);
            var textRightSize = ImGui.CalcTextSize(textRight);

            DrawOutlinedText(textRight,
                new Vector2(cursorPos.X + TargetBarWidth - textRightSize.X - 5 + TargetBarTextRightXOffset,
                    cursorPos.Y - 22 + TargetBarTextRightYOffset));

            DrawTargetOfTargetBar(target.TargetObject);
        }

        protected virtual void DrawFocusBar() {
            var focus = TargetManager.FocusTarget;
            if (focus is null) {
                return;
            }

            var barSize = new Vector2(FocusBarWidth, FocusBarHeight);

            var cursorPos = new Vector2(CenterX - FocusBarXOffset - HealthBarWidth - FocusBarWidth - 2, CenterY + FocusBarYOffset);
            ImGui.SetCursorPos(cursorPos);

            var drawList = ImGui.GetWindowDrawList();
            if (focus is not Character actor) {
                var friendly = PluginConfiguration.NPCColorMap["friendly"];
                drawList.AddRectFilled(cursorPos, cursorPos + barSize, friendly["background"]);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(FocusBarWidth, FocusBarHeight),
                    friendly["gradientLeft"], friendly["gradientRight"], friendly["gradientRight"], friendly["gradientLeft"]
                );
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
                DrawTargetShield(focus, cursorPos, barSize, true);
            }
            else {
                var colors = DetermineTargetPlateColors(actor);
                drawList.AddRectFilled(cursorPos, cursorPos + barSize, colors["background"]);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2((float) FocusBarWidth * actor.CurrentHp / actor.MaxHp, FocusBarHeight),
                    colors["gradientLeft"], colors["gradientRight"], colors["gradientRight"], colors["gradientLeft"]
                );
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);

                DrawTargetShield(focus, cursorPos, barSize, true);
            }

            var text = TextTags.GenerateFormattedTextFromTags(focus, PluginConfiguration.FocusBarText);
            var textSize = ImGui.CalcTextSize(text);
            DrawOutlinedText(text,
                new Vector2(
                    cursorPos.X + FocusBarWidth / 2f - textSize.X / 2f + FocusBarTextXOffset,
                    cursorPos.Y - 22 + FocusBarTextYOffset
                )
            );
        }
        
        protected virtual void DrawTargetOfTargetBar(GameObject targetObject) {
            if (targetObject is not Character actor) {
                return;
            }

            var barSize = new Vector2(ToTBarWidth, ToTBarHeight);
            var colors = DetermineTargetPlateColors(actor);
            var text = TextTags.GenerateFormattedTextFromTags(targetObject, PluginConfiguration.ToTBarText);
            var textSize = ImGui.CalcTextSize(text);
            var cursorPos = new Vector2(CenterX + ToTBarXOffset + TargetBarWidth + 2, CenterY + ToTBarYOffset);

            ImGui.SetCursorPos(cursorPos);

            var drawList = ImGui.GetWindowDrawList();

            // Basically make an invisible box for BeginChild to work properly.
            ImGuiWindowFlags windowFlags = 0;
            windowFlags |= ImGuiWindowFlags.NoBackground;
            windowFlags |= ImGuiWindowFlags.NoTitleBar;
            windowFlags |= ImGuiWindowFlags.NoMove;
            windowFlags |= ImGuiWindowFlags.NoDecoration;

            ImGui.SetNextWindowPos(cursorPos);
            ImGui.SetNextWindowSize(barSize);

            ImGui.Begin("target_of_target_bar", windowFlags);
            if (ImGui.BeginChild("target_of_target_bar", barSize)) {
                drawList.AddRectFilled(cursorPos, cursorPos + barSize, colors["background"]);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2((float) ToTBarWidth * actor.CurrentHp / actor.MaxHp, ToTBarHeight),
                    colors["gradientLeft"], colors["gradientRight"], colors["gradientRight"], colors["gradientLeft"]
                );
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);

                if (ImGui.GetIO().MouseClicked[0] && ImGui.IsMouseHoveringRect(cursorPos, cursorPos + barSize)) {
                    TargetManager.SetTarget(targetObject);
                }
            }
            
            ImGui.EndChild();
            ImGui.End();

            DrawTargetShield(targetObject, cursorPos, barSize, true);

            DrawOutlinedText(text,
                new Vector2(
                    cursorPos.X + ToTBarWidth / 2f - textSize.X / 2f + ToTBarTextXOffset,
                    cursorPos.Y - 22 + ToTBarTextYOffset
                )
            );
        }

        protected virtual unsafe void DrawCastBar() {
            if (!PluginConfiguration.ShowCastBar) {
                return;
            }

            Debug.Assert(ClientState.LocalPlayer != null,  "ClientState.LocalPlayer != null");
            var actor = ClientState.LocalPlayer;
            var battleChara = (BattleChara*) actor.Address;
            var castInfo = battleChara->SpellCastInfo;
            var isCasting = castInfo.IsCasting > 0;
            if (!isCasting) return;
            
            var currentCastId = castInfo.ActionID;
            var currentCastType = castInfo.ActionType;
            var currentCastTime = castInfo.CurrentCastTime;
            var totalCastTime = castInfo.TotalCastTime;

            if (_lastPlayerUsedCast != null)
            {
                if (!(_lastPlayerUsedCast.CastId == currentCastId && _lastPlayerUsedCast.ActionType == currentCastType)) {
                    _lastPlayerUsedCast = new LastUsedCast(currentCastId, currentCastType, castInfo, DataManager, UiBuilder);
                }
            }
            else {
                _lastPlayerUsedCast = new LastUsedCast(currentCastId, currentCastType, castInfo, DataManager, UiBuilder);
            }
            
            var castText = _lastPlayerUsedCast.ActionText;

            var castPercent = 100f / totalCastTime * currentCastTime;
            var castScale = castPercent / 100f;
            var castTime = Math.Round((totalCastTime - totalCastTime * castScale), 1).ToString(CultureInfo.InvariantCulture);
            var barSize = new Vector2(CastBarWidth, CastBarHeight);
            var cursorPos = new Vector2(
                CenterX + CastBarXOffset - CastBarWidth / 2f,
                CenterY + CastBarYOffset
            );

            ImGui.SetCursorPos(cursorPos);

            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(cursorPos, cursorPos + barSize, 0x88000000);

            if (PluginConfiguration.SlideCast) {
                var slideColor = PluginConfiguration.CastBarColorMap["slidecast"];
                var slideCastScale = PluginConfiguration.SlideCastTime / 10f / totalCastTime / 100f;

                drawList.AddRectFilledMultiColor(
                    cursorPos + barSize - new Vector2(barSize.X * slideCastScale, barSize.Y), cursorPos + barSize,
                    slideColor["gradientLeft"], slideColor["gradientRight"], slideColor["gradientRight"], slideColor["gradientLeft"]
                );
            }

            var castColor = PluginConfiguration.CastBarColorMap["castbar"];
            if (PluginConfiguration.ColorCastBarByJob)
            {
                PluginConfiguration.JobColorMap.TryGetValue(ClientState.LocalPlayer.ClassJob.Id, out castColor);
                castColor ??= PluginConfiguration.CastBarColorMap["castbar"];
            }
            drawList.AddRectFilledMultiColor(
                cursorPos, cursorPos + new Vector2(barSize.X * castScale, barSize.Y),
                castColor["gradientLeft"], castColor["gradientRight"], castColor["gradientRight"], castColor["gradientLeft"]
            );
            drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
            
            if (PluginConfiguration.ShowActionIcon && _lastPlayerUsedCast.HasIcon) {
                ImGui.Image(_lastPlayerUsedCast.IconTexture.ImGuiHandle, new Vector2(CastBarHeight, CastBarHeight));
                drawList.AddRect(cursorPos, cursorPos + new Vector2(CastBarHeight, CastBarHeight), 0xFF000000);
            }

            var castTextSize = ImGui.CalcTextSize(castText);
            var castTimeTextSize = ImGui.CalcTextSize(castTime);

            if (PluginConfiguration.ShowCastTime) {
                DrawOutlinedText(
                    castTime,
                    new Vector2(cursorPos.X + CastBarWidth - castTimeTextSize.X - 5, cursorPos.Y + CastBarHeight / 2f - castTimeTextSize.Y / 2f)
                );
            }

            if (PluginConfiguration.ShowActionName) {
                DrawOutlinedText(
                    castText,
                    new Vector2(
                        cursorPos.X + (PluginConfiguration.ShowActionIcon && _lastPlayerUsedCast.HasIcon ? CastBarHeight : 0) + 5,
                        cursorPos.Y + CastBarHeight / 2f - castTextSize.Y / 2f
                    )
                );
            }
        }
        
        protected virtual unsafe void DrawTargetCastBar() {
            var actor = TargetManager.SoftTarget ?? TargetManager.Target;
            if (!PluginConfiguration.ShowTargetCastBar || actor is null) {
                return;
            }

            if (actor is not Character) return;

            // GameObject.CurrentCastId (for 6.0)
            var battleChara = (BattleChara*) actor.Address;
            var castInfo = battleChara->SpellCastInfo;

            var isCasting = castInfo.IsCasting > 0;
            if (!isCasting) return;
            var currentCastId = castInfo.ActionID;
            var currentCastType = castInfo.ActionType;
            var currentCastTime = castInfo.CurrentCastTime;
            var totalCastTime = castInfo.TotalCastTime;
            
            if (_lastTargetUsedCast != null) {
                if (!(_lastTargetUsedCast.CastId == currentCastId && _lastTargetUsedCast.ActionType == currentCastType)) {
                    _lastTargetUsedCast = new LastUsedCast(currentCastId, currentCastType, castInfo, DataManager, UiBuilder);
                }
            }
            else {
                _lastTargetUsedCast = new LastUsedCast(currentCastId, currentCastType, castInfo, DataManager, UiBuilder);
            }

            var castText = _lastTargetUsedCast.ActionText;

            var castPercent = 100f / totalCastTime * currentCastTime;
            var castScale = castPercent / 100f;

            var castTime = Math.Round((totalCastTime - totalCastTime * castScale), 1).ToString(CultureInfo.InvariantCulture);
            var barSize = new Vector2(TargetCastBarWidth, TargetCastBarHeight);
            var cursorPos = new Vector2(
                CenterX + PluginConfiguration.TargetCastBarXOffset - TargetCastBarWidth / 2f,
                CenterY + PluginConfiguration.TargetCastBarYOffset
            );

            ImGui.SetCursorPos(cursorPos);

            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(cursorPos, cursorPos + barSize, 0x88000000);

            var castColor = PluginConfiguration.CastBarColorMap["targetcastbar"];

            if (PluginConfiguration.ColorCastBarByDamageType) {
                switch (_lastTargetUsedCast.DamageType) {
                    case DamageType.Physical:
                    case DamageType.Blunt:
                    case DamageType.Slashing:
                    case DamageType.Piercing:
                        castColor = PluginConfiguration.CastBarColorMap["targetphysicalcastbar"];
                        break;
                    case DamageType.Magic:
                        castColor = PluginConfiguration.CastBarColorMap["targetmagicalcastbar"];
                        break;
                    case DamageType.Darkness:
                        castColor = PluginConfiguration.CastBarColorMap["targetdarknesscastbar"];
                        break;
                    case DamageType.Unknown:
                    case DamageType.LimitBreak:
                        castColor = PluginConfiguration.CastBarColorMap["targetcastbar"];
                        break;
                    default:
                        castColor = PluginConfiguration.CastBarColorMap["targetcastbar"];
                        break;
                }
            }

            if (PluginConfiguration.ShowTargetInterrupt && _lastTargetUsedCast.Interruptable) {
                castColor = PluginConfiguration.CastBarColorMap["targetinterruptcastbar"];
            }
            
            drawList.AddRectFilledMultiColor(
                cursorPos, cursorPos + new Vector2(barSize.X * castScale, barSize.Y),
                castColor["gradientLeft"], castColor["gradientRight"], castColor["gradientRight"], castColor["gradientLeft"]
            );
            drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);

            if (PluginConfiguration.ShowTargetActionIcon && _lastTargetUsedCast.HasIcon) {
                ImGui.Image(_lastTargetUsedCast.IconTexture.ImGuiHandle, new Vector2(TargetCastBarHeight, TargetCastBarHeight));
                drawList.AddRect(cursorPos, cursorPos + new Vector2(TargetCastBarHeight, TargetCastBarHeight), 0xFF000000);
            }

            var castTextSize = ImGui.CalcTextSize(castText);
            var castTimeTextSize = ImGui.CalcTextSize(castTime);

            if (PluginConfiguration.ShowTargetCastTime) {
                DrawOutlinedText(
                    castTime,
                    new Vector2(cursorPos.X + TargetCastBarWidth - castTimeTextSize.X - 5, cursorPos.Y + TargetCastBarHeight / 2f - castTimeTextSize.Y / 2f)
                );
            }

            if (PluginConfiguration.ShowTargetActionName) {
                DrawOutlinedText(
                    castText,
                    new Vector2(
                        cursorPos.X + (PluginConfiguration.ShowTargetActionIcon && _lastTargetUsedCast.HasIcon ? TargetCastBarHeight : 0) + 5,
                        cursorPos.Y + TargetCastBarHeight / 2f - castTextSize.Y / 2f
                    )
                );
            }
        }

        protected virtual void DrawTargetShield(GameObject actor, Vector2 cursorPos, Vector2 targetBar, bool leftToRight)
        {
            if (!PluginConfiguration.ShieldEnabled) {
                return;
            }

            if (actor.ObjectKind is not ObjectKind.Player) {
                return;
            }

            var shieldColor = PluginConfiguration.MiscColorMap["shield"];
            var shield = ActorShieldValue(actor);
            
            if (Math.Abs(shield) < 0) {
                return;
            }

            var drawList = ImGui.GetWindowDrawList();
            var y = PluginConfiguration.ShieldHeightPixels
                ? PluginConfiguration.ShieldHeight
                : targetBar.Y / 100 * PluginConfiguration.ShieldHeight;
            
            drawList.AddRectFilledMultiColor(
                cursorPos, cursorPos + new Vector2(targetBar.X * shield, y),
                shieldColor["gradientLeft"], shieldColor["gradientRight"], shieldColor["gradientRight"], shieldColor["gradientLeft"]
            );
            drawList.AddRect(cursorPos, cursorPos + targetBar, 0xFF000000);
        }

        protected virtual void DrawTankStanceIndicator()
        {
            Debug.Assert(ClientState.LocalPlayer != null, "ClientState.LocalPlayer != null");
            var tankStanceBuff = ClientState.LocalPlayer.StatusList.Where(o => 
                o.StatusId == 79   ||   // IRON WILL
                o.StatusId == 91   ||   // DEFIANCE
                o.StatusId == 392  ||   // ROYAL GUARD
                o.StatusId == 393  ||   // IRON WILL
                o.StatusId == 743  ||   // GRIT
                o.StatusId == 1396 ||   // DEFIANCE
                o.StatusId == 1397 ||   // GRIT
                o.StatusId == 1833      // ROYAL GUARD
            );

            var offset = PluginConfiguration.TankStanceIndicatorWidth + 1;
            if (tankStanceBuff.Count() != 1) {
                var barSize = new Vector2(HealthBarHeight > HealthBarWidth ? HealthBarWidth : HealthBarHeight, HealthBarHeight);
                var cursorPos = new Vector2(CenterX - HealthBarWidth - HealthBarXOffset - offset, CenterY + HealthBarYOffset + offset);
                ImGui.SetCursorPos(cursorPos);

                var drawList = ImGui.GetWindowDrawList();

                drawList.AddRectFilled(cursorPos, cursorPos + barSize, 0x88000000);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + barSize,
                    0xFF2000FC, 0xFF2000FC, 0xFF2000FC, 0xFF2000FC
                );
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
            }
            else {
                var barSize = new Vector2(HealthBarHeight > HealthBarWidth ? HealthBarWidth : HealthBarHeight, HealthBarHeight);
                var cursorPos = new Vector2(CenterX - HealthBarWidth - HealthBarXOffset - offset, CenterY + HealthBarYOffset + offset);
                ImGui.SetCursorPos(cursorPos);

                var drawList = ImGui.GetWindowDrawList();

                drawList.AddRectFilled(cursorPos, cursorPos + barSize, 0x88000000);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + barSize,
                    0xFFE6CD00, 0xFFE6CD00, 0xFFE6CD00, 0xFFE6CD00
                );
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
            }
        }

        protected virtual void DrawMPTicker()
        {
            if (!PluginConfiguration.MPTickerEnabled) {
                return;
            }

            if (MPTickerHideOnFullMp) {
                Debug.Assert(ClientState.LocalPlayer != null, "ClientState.LocalPlayer != null");
                var actor = ClientState.LocalPlayer;
                if (actor.CurrentMp >= actor.MaxMp) {
                    return;
                }
            }

            _mpTickHelper ??= new MpTickHelper(Framework, ClientState);

            var now = ImGui.GetTime();
            var scale = (float)((now - _mpTickHelper.LastTick) / MpTickHelper.ServerTickRate);
            if (scale <= 0) {
                return;
            }
           
            if (scale > 1) {
                scale = 1;
            }

            var fullSize = new Vector2(MPTickerWidth, MPTickerHeight);
            var barSize = new Vector2(Math.Max(1f, MPTickerWidth * scale), MPTickerHeight);
            var position = new Vector2(CenterX + MPTickerXOffset - MPTickerWidth / 2f, CenterY + MPTickerYOffset);
            var colors = PluginConfiguration.MiscColorMap["mpTicker"];

            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(position, position + fullSize, 0x88000000);
            drawList.AddRectFilledMultiColor(position, position + barSize,
                colors["gradientLeft"], colors["gradientRight"], colors["gradientRight"], colors["gradientLeft"]
            );

            if (MPTickerShowBorder) {
                drawList.AddRect(position, position + fullSize, 0xFF000000);
            }
        }
        
        protected virtual void DrawGCDIndicator() {
            if (!PluginConfiguration.GCDIndicatorEnabled || ClientState.LocalPlayer is null) {
                return;
            }

            GCDHelper.GetGCDInfo(ClientState.LocalPlayer, out var elapsed, out var total);
            if (total == 0) return;

            var scale = elapsed / total;
            if (scale <= 0) return;

            var fullSize = new Vector2(GCDIndicatorWidth, GCDIndicatorHeight);
            var barSize = new Vector2(Math.Max(1f, GCDIndicatorWidth * scale), GCDIndicatorHeight);
            var position = new Vector2(CenterX + GCDIndicatorXOffset - GCDIndicatorWidth / 2f, CenterY + GCDIndicatorYOffset);
            var colors = PluginConfiguration.MiscColorMap["mpTicker"];

            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(position, position + fullSize, 0x88000000);
            drawList.AddRectFilledMultiColor(position, position + barSize,
                colors["gradientLeft"], colors["gradientRight"], colors["gradientRight"], colors["gradientLeft"]
            );

            if (GCDIndicatorShowBorder) {
                drawList.AddRect(position, position + fullSize, 0xFF000000);
            }
        }

        protected unsafe virtual float ActorShieldValue(GameObject actor) {
            return Math.Min(*(int*) (actor.Address + 0x1997), 100) / 100f;
        }

        protected Dictionary<string, uint> DetermineTargetPlateColors(Character actor) {
            var colors = PluginConfiguration.NPCColorMap["neutral"];

            // Still need to figure out the "orange" state; aggroed but not yet attacked.
            switch (actor.ObjectKind) {
                case ObjectKind.Player:
                    colors = PluginConfiguration.JobColorMap.GetValueOrDefault(actor.ClassJob.Id, PluginConfiguration.NPCColorMap["neutral"]);
                    break;

                case ObjectKind.BattleNpc when (actor.StatusFlags & StatusFlags.InCombat) == StatusFlags.InCombat:
                    colors = PluginConfiguration.NPCColorMap["hostile"];
                    break;

                case ObjectKind.BattleNpc:
                {
                    if (!IsHostileMemory((BattleNpc) actor)) {
                        colors = PluginConfiguration.NPCColorMap["friendly"];
                    }

                    break;
                }
            }

            return colors;
        }

        protected void DrawOutlinedText(string text, Vector2 pos) {
            DrawHelper.DrawOutlinedText(text, pos);
        }

        protected void DrawOutlinedText(string text, Vector2 pos, Vector4 color, Vector4 outlineColor) {
            DrawHelper.DrawOutlinedText(text, pos, color, outlineColor);
        }

        public void Draw() {
            if (!ShouldBeVisible() || ClientState.LocalPlayer == null) {
                return;
            }

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);

            var begin = ImGui.Begin(
                "DelvUI",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBringToFrontOnFocus
            );

            if (!begin) {
                return;
            }

            DrawGenericElements();

            Draw(true);

            ImGui.End();
        }

        protected void DrawGenericElements()
        {
            DrawHealthBar();
            DrawPrimaryResourceBar();
            DrawTargetBar();
            DrawFocusBar();
            DrawCastBar();
            DrawTargetCastBar();
            DrawMPTicker();
            DrawGCDIndicator();
        }

        protected abstract void Draw(bool _);

        protected virtual void HandleProperties() {
            Debug.Assert(ClientState.LocalPlayer != null, "ClientState.LocalPlayer != null");
            var actor = ClientState.LocalPlayer;
        }

        protected virtual unsafe bool ShouldBeVisible() {
            if (PluginConfiguration.HideHud) {
                return false;
            }

            if (IsVisible) {
                return true;
            }

            var parameterWidget = (AtkUnitBase*) GameGui.GetAddonByName("_ParameterWidget", 1);
            var fadeMiddleWidget = (AtkUnitBase*) GameGui.GetAddonByName("FadeMiddle", 1);
            
            // Display HUD only if parameter widget is visible and we're not in a fade event
            return ClientState.LocalPlayer == null || parameterWidget == null || fadeMiddleWidget == null || !parameterWidget->IsVisible || fadeMiddleWidget->IsVisible;
        }

        private static unsafe bool IsHostileMemory(BattleNpc npc) {
            return (npc.BattleNpcKind == BattleNpcSubKind.Enemy || (int) npc.BattleNpcKind == 1)
                   && *(byte*) (npc.Address + 0x1980) != 0
                   && *(byte*) (npc.Address + 0x193C) != 1;
        }
    }
}
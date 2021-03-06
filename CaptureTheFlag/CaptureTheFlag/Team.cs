﻿using CaptureTheFlag.Map;
using CaptureTheFlag.Textdraw;
using SampSharp.GameMode;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using System;
using System.Collections.Generic;
using System.Text;
using CaptureTheFlag.Constants;
using CaptureTheFlag.PropertiesPlayer;

namespace CaptureTheFlag
{
    public class Team
    {
        public TeamID Id { get; private set; }
        public int Members => Players.Count;
        public int Score { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Skin { get; private set; }
        public string NameTeam { get; private set; }
        public string NameColor { get; private set; }
        public string ColorEnglish { get; private set; }
        public string ColorGameText { get; private set; }
        public string OtherColor { get; private set; }
        public Flag Flag { get; private set; }
        public Team TeamRival { get; set; }
        public TextDraw TdScore { get; private set; }
        public DynamicMapIcon Icon { get; private set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public Pickup Arrow { get; set; }

        public Team(
            int skin, 
            string otherColor, 
            string colorGameText, 
            TextDraw tdscore, 
            TeamID teamid, 
            string name, 
            string namecolor, 
            string colorEnglish, 
            Flag flag, 
            int interior
            )
        {
            Skin = skin;
            OtherColor = otherColor;
            ColorGameText = colorGameText;
            TdScore = tdscore;
            Id = teamid;
            NameTeam = name;
            NameColor = namecolor;
            ColorEnglish = colorEnglish;
            Flag = flag;
            Icon = new DynamicMapIcon(Flag.PositionBase, 0) { StreamDistance = 5000f, Interior = interior, Color = Flag.ColorHex};
        }

        public bool IsFull() => Members > TeamRival.Members;
        public void UpdateTdScore() => TdScore.Text = $"{ColorGameText}{NameTeam}: {Score}";

        public void ResetStats()
        {
            Score = 0;
            Kills = 0;
            Deaths = 0;
            UpdateTdScore();
        }

        public void UpdateIcon()
        {
            Icon.Position = Flag.PositionBase;
            Icon.Interior = CurrentMap.Interior;
        }
        
        public bool GetMessageTeamEnable(out string message, bool msgComplete = true)
        {
            bool full;
            message = (full = IsFull()) ?
                (msgComplete ? $"~y~{NameTeam}~n~~r~ not available" : "not available") :
                (msgComplete ? $"~y~{NameTeam}~n~~r~ available" : "available");
            return full;
        }

        public void ExecuteAction(Player player, Pickup pickup)
        {
            if (Flag.IsPositionBase)
            {
                if (player.PlayerTeam.Id == TeamRival.Id)
                {
                    Captured(player);
                    pickup.Dispose();
                }
                else if (player == TeamRival.Flag.PlayerCaptured)
                    TeamRival.Carry(player);
            }
            else
            {
                if (player.PlayerTeam.Id == Id)
                    Recover(player);
                else
                    Captured(player, false);
                pickup.Dispose();
            }
        }

        public void Captured(Player player, bool takeInPosBase = true)
        {
            Flag.IsPositionBase = false;
            Flag.AttachedObject(player);
            Flag.PlayerCaptured = player;
            player.JumpTime = 0;
            Arrow = Pickup.Create(1318, 1, Flag.PositionBase);
            Arrow.PickUp += (sender, e) =>
            {
                if(e.Player.Team == (int)Id)
                    e.Player.GameText($"{ColorGameText}recovers the {ColorEnglish} flag!", 5000, 3);
            };
            if (player.IsEnableSpeed())
            {
                player.SpeedTime = 0;
                player.ClearAnimations();
            }
            if(player.IsEnableInvisible())
            {
                player.InvisibleTime = 0;
                player.DisableInvisibility();
            }
            if (player.IsInvisible)
            {
                player.DisableInvisibility();
                player.IsInvisible = false;
            }
            if (takeInPosBase)
            {
                BasePlayer.SendClientMessageToAll($"{OtherColor}[Team {NameTeam}]: {player.Name} capturó la bandera {NameColor} del equipo {NameTeam}.");
                player.SendClientMessage($"{Color.Pink}[!]: {Color.White}Capturaste la bandera, debes llevarla a tu base.");
                player.UpdateAdrenaline(4, "capturar la bandera");
                BasePlayer.GameTextForAll($"{ColorGameText}{ColorEnglish} flag captured!", 5000, 3);
            }
            else
            {
                BasePlayer.SendClientMessageToAll($"{OtherColor}[Team {NameTeam}]: {player.Name} tomó la bandera {NameColor} del equipo {NameTeam}.");
                player.SendClientMessage($"{Color.Pink}[!]: {Color.White}Debes llevar esa bandera a tu base.");
                BasePlayer.GameTextForAll($"{ColorGameText}{ColorEnglish} flag tooked!", 5000, 3);
            }
        }

        public void Carry(Player player)
        {
            BasePlayer.SendClientMessageToAll($"{OtherColor}[Team {NameTeam}]: {player.Name} llevó la bandera {NameColor} del equipo {NameTeam} a su base.");
            BasePlayer.GameTextForAll($"{TeamRival.ColorGameText}+1 score team {TeamRival.NameTeam}", 5000, 3);
            player.RemoveAttachedObject(0);
            Flag.Create();
            Arrow.Dispose();
            Flag.PlayerCaptured = null;
            Flag.IsPositionBase = true;
            ++TeamRival.Score;
            TeamRival.UpdateTdScore();
            player.UpdateAdrenaline(10, "llevar la bandera tu base");
            player.UpdateData("droppedFlags", ++player.Data.DroppedFlags);
            foreach(Player player1 in player.PlayerTeam.Players)
                if(player != player1)
                    player1.UpdateAdrenaline(3, "ayudar a capturar la bandera");
        }

        public void Recover(Player player)
        {
            Flag.IsPositionBase = true;
            Flag.Create();
            Arrow.Dispose();
            BasePlayer.SendClientMessageToAll($"{OtherColor}[Team {NameTeam}]: {player.Name} recuperó la bandera {NameColor} del equipo {NameTeam}.");
            BasePlayer.GameTextForAll($"{ColorGameText}{ColorEnglish} flag recovered!", 5000, 3);
            player.UpdateAdrenaline(4, "recuperar la bandera");
        }

        public void Drop(Player player, Player killer)
        {
            Drop(player);
            if (killer != null)
                killer.UpdateAdrenaline(4, "matar al portador");
        }

        public void Drop(Player player)
        {
            Flag.Create(player);
            Flag.PlayerCaptured = null;
            Flag.IsPositionBase = false;
            BasePlayer.SendClientMessageToAll($"{OtherColor}[Team {NameTeam}]: {player.Name} dejó caer la bandera {NameColor} del equipo {NameTeam}.");
            BasePlayer.GameTextForAll($"{ColorGameText}{ColorEnglish} flag dropped!", 5000, 3);
            player.RemoveAttachedObject(0);
        }
    }
}

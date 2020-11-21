﻿using CaptureTheFlag.Textdraw;
using SampSharp.GameMode;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaptureTheFlag.Map
{
    public static class CurrentMap
    {
        /* *** Constants */
        public static int MAX_SPAWNS;
        public static int MAX_TIME_ROUND;
        public static int MAX_TIME_LOADING;
        public static int MAX_MAPS;
        /* ** */

        public static int timeLeft;
        private static readonly Random random = new Random();
        public static int Id { get; set; }
        public static int Interior { get; set; }
        public static bool IsLoading { get; set; }
        public static int ForceMap { get; set; } = -1;
        public static SpawnPoint[,] spawns;
        public static string[] mapName;

        public static void StartTimer()
        {
            var timer = new Timer(1000, true);
            TextDraw tdTimeLeft = TextDrawGlobal.TdTimeLeft;
            FileRead.ConfigMapRead();
            int timeLoading = MAX_TIME_LOADING;
            timeLeft = MAX_TIME_ROUND;
            spawns = new SpawnPoint[2, MAX_SPAWNS];
            mapName = new string[MAX_MAPS];
            FileRead.NamesMapRead();
            Id = random.Next(MAX_MAPS);
            for (int i = 0; i < MAX_SPAWNS; ++i)
                spawns[(int)TeamID.Alpha, i] = new SpawnPoint();
            for (int i = 0; i < MAX_SPAWNS; ++i)
                spawns[(int)TeamID.Beta, i] = new SpawnPoint();
            FileRead.SpawnPositionRead();
            timer.Tick += (sender, e) =>
            {
                if (timeLeft < 0)
                {
                    if (timeLoading == MAX_TIME_LOADING)
                    {
                        IsLoading = true;
                        if (GameMode.TeamAlpha.Score > GameMode.TeamBeta.Score)
                            BasePlayer.SendClientMessageToAll(Color.Red, $"[Round]: {Color.Yellow}Esta partida la ganó el equipo Alpha.");
                        else if (GameMode.TeamAlpha.Score == GameMode.TeamBeta.Score)
                            BasePlayer.SendClientMessageToAll(Color.Red, $"[Round]: {Color.Yellow}Hubo un empate! Ningún equipo ganó.");
                        else
                            BasePlayer.SendClientMessageToAll(Color.Red, $"[Round]: {Color.Yellow}Esta partida la ganó el equipo Beta.");

                        Server.SendRconCommand($"unloadfs {GetMapName(Id)}");
                        /*
                            This verifies if any player has actually forced the map change. 
                            Therefore, the sequence for the "map change" is not followed.
                        */
                        Id = (ForceMap == -1) ? (Id + 1) % MAX_MAPS : ForceMap;
                        foreach (Player player in BasePlayer.GetAll<Player>())
                            if (player.Team != BasePlayer.NoTeam)
                                player.ToggleControllable(false);

                        BasePlayer.SendClientMessageToAll(Color.Yellow, $"** En {MAX_TIME_LOADING} segundos se cargará el próximo mapa: {Color.Red}{GetMapName(Id)}");
                        Flag.RemoveAll();
                        GameMode.TeamAlpha.Flag.DeletePlayerCaptured();
                        GameMode.TeamBeta.Flag.DeletePlayerCaptured();
                        FileRead.SpawnPositionRead();
                        GameMode.TeamAlpha.Flag.Create(FileRead.FlagPositionRead("Red"));
                        GameMode.TeamBeta.Flag.Create(FileRead.FlagPositionRead("Blue"));
                        GameMode.TeamAlpha.UpdateUtils();
                        GameMode.TeamBeta.UpdateUtils();
                        GameMode.TeamAlpha.ResetStats();
                        GameMode.TeamBeta.ResetStats();
                        Server.SendRconCommand($"loadfs {GetMapName(Id)}");
                        Server.SendRconCommand($"mapname {GetMapName(Id)}");
                    }
                    else if (timeLoading < 0)
                    {
                        BasePlayer.GameTextForAll("_", 1000, 4);
                        IsLoading = false;
                        ForceMap = -1;
                        BasePlayer.SendClientMessageToAll(Color.Yellow, "** El mapa se cargó con éxito!");
                        foreach (Player player in BasePlayer.GetAll<Player>())
                        {
                            player.Kills = 0;
                            player.Deaths = 0;
                            player.KillingSprees = 0;
                            player.Adrenaline = 0;

                            if (player.Team != BasePlayer.NoTeam)
                            {
                                player.ToggleControllable(true);
                                player.SetForceClass();
                            }
                        }
                        timeLeft = MAX_TIME_ROUND;
                        timeLoading = MAX_TIME_LOADING;
                        return;
                    }
                    BasePlayer.GameTextForAll($"Cargando Mapa... ({timeLoading})", 99999999, 3);
                    --timeLoading;
                }
                else
                {
                    tdTimeLeft.Text = $"{timeLeft / 60:D2}:{timeLeft % 60:D2}";
                    tdTimeLeft.Show();
                    --timeLeft;
                }
            };
        }

        public static void SetPlayerPosition(Player player)
        {
            int teamid = player.Team;
            int rand = random.Next(MAX_SPAWNS);
            player.Position = new Vector3(spawns[teamid, rand].X, spawns[teamid, rand].Y, spawns[teamid, rand].Z);
            player.Angle = spawns[teamid, rand].Angle;
            player.Interior = Interior;
        }

        public static string GetMapName(int mapid)
        {
            return mapName[mapid];
        }

        public static string GetCurrentMap()
        {
            return mapName[Id];
        }

    }
}
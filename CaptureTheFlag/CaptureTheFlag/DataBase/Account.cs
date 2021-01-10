﻿using MySql.Data.MySqlClient;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using System;
using System.Collections.Generic;
using System.Text;
using CaptureTheFlag.Constants;
using SampSharp.GameMode.World;
using CaptureTheFlag.PropertiesPlayer;
using CaptureTheFlag.Command.Public;
using static CaptureTheFlag.DataBase.DBCommand;

namespace CaptureTheFlag.DataBase
{
    public partial class Account
    {
        public Account()
        {
            BaseMode.Instance.PlayerConnected += (sender, e) =>
            {
                var player = sender as Player;
                if (Exists(player.Name))
                {
                    player.Account = AccountState.Login;
                    ShowDialogLogin(player);
                }
                else
                {
                    player.Account = AccountState.Register;
                    ShowDialogRegister(player);
                }
            };
        }

        public static void ShowDialogRegister(Player player)
        {
            var register = new InputDialog($"{Color.Yellow}Regístrate", $"Esta cuenta no está registrada.\nIngrese una contraseña:", true, "Aceptar", "");
            register.Show(player);
            register.Response += (sender, e) =>
            {
                if (e.DialogButton == DialogButton.Left)
                {
                    Validate.IsEmpty(player, register, e.InputText);
                    Validate.PasswordRange(player, register, e.InputText);
                    Create(player, e.InputText);
                    CmdPublic.Help(player);
                    player.Account = AccountState.None;
                    player.SendClientMessage(Color.Orange, $"[Cuenta]: {Color.Yellow}Te has registrado de forma exitosa. {Color.Orange}Contraseña: {e.InputText}");
                }
                else
                    register.Show(player);

            };
        }

        public static void ShowDialogLogin(Player player)
        {
            var login = new InputDialog($"{Color.Orange}Iniciar Sesión", "Esta cuenta sí está registrada.\nIngrese una contraseña:", true, "Aceptar", "");
            login.Show(player);
            Load(player, out var password);
            login.Response += (sender, e) =>
            {
                if (e.DialogButton == DialogButton.Left)
                {
                    Validate.IsEmpty(player, login, e.InputText);
                    Validate.PasswordRange(player, login, e.InputText);
                    if (password != Encrypt(e.InputText))
                    {
                        login.Message = "La contraseña que ingresaste es incorrecta.\nIngrese una contraseña:";
                        login.Show(player);
                        return;
                    }
                    LoadAdminLevel(player);
                    LoadVipLevel(player);
                    CmdPublic.StatsPlayer(player);
                    player.Account = AccountState.None;
                    player.SendClientMessage(Color.Orange, $"[Cuenta]: {Color.Yellow}Has iniciado sesión de forma exitosa!");
                    Player.AddAV(player);
                }
                else
                    login.Show(player);
            };
        }

        public static bool Exists(string playername)
        {
            MySqlDataReader reader;
            bool exists;
            cmd.CommandText = "SELECT namePlayer FROM Players WHERE namePlayer = @namePlayer;";
            cmd.Parameters.AddWithValue("@namePlayer", playername);
            reader = cmd.ExecuteReader();
            exists = reader.Read();
            cmd.Parameters.Clear();
            reader.Close();
            return exists;
        }

        public static void Create(Player player, string password)
        {
            player.Data.RegistryDate = DateTime.Now;
            cmd.CommandText = "INSERT INTO Players(namePlayer, pass, totalKills, totalDeaths, killingSprees, levelGame, droppedFlags, headshots, registryDate) VALUES(@namePlayer, SHA2(@pass, 256), 0, 0, 0, 1, 0, 0, @registryDate);";
            cmd.Parameters.AddWithValue("@namePlayer", player.Name);
            cmd.Parameters.AddWithValue("@pass", password);
            cmd.Parameters.AddWithValue("@registryDate", player.Data.RegistryDate);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
        }

        public static void Load(Player player, out string password)
        {
            MySqlDataReader reader;
            cmd.CommandText = "SELECT * FROM Players WHERE namePlayer = @namePlayer;";
            cmd.Parameters.AddWithValue("@namePlayer", player.Name);
            reader = cmd.ExecuteReader();
            reader.Read();
            password = reader.GetString("pass"); 
            player.Data.TotalKills = reader.GetInt32("totalKills");
            player.Data.TotalDeaths = reader.GetInt32("totalDeaths");
            player.Data.KillingSprees = reader.GetInt32("killingSprees");
            player.Data.LevelGame = reader.GetInt32("levelGame");
            player.Data.DroppedFlags = reader.GetInt32("droppedFlags");
            player.Data.Headshots = reader.GetInt32("headshots");
            player.Data.RegistryDate = reader.GetDateTime("registryDate");
            cmd.Parameters.Clear();
            reader.Close();
        }

        public static string Encrypt(string text)
        {
            cmd.CommandText = "SELECT SHA2(@text, 256);";
            cmd.Parameters.AddWithValue("@text", text);
            string str = (string)cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return str;
        }
    }  
}
   
using CaptureTheFlag.Command.Public;
using CaptureTheFlag.PropertiesPlayer;
using CaptureTheFlag.Textdraw;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.World;
using System;

namespace CaptureTheFlag.Events
{
    public partial class GameMode : BaseMode
    {
        protected override void OnPlayerDisconnected(BasePlayer sender, DisconnectEventArgs e)
        {
            base.OnPlayerDisconnected(sender, e);
            var player = sender as Player;
            BasePlayer.SendDeathMessageToAll(null, player, Weapon.Disconnect);
            if (player.IsCapturedFlag())
                player.Drop();
            if (player.Team != BasePlayer.NoTeam)
            {
                Player.Remove(player);
                TextDrawGlobal.UpdateCountUsers();
            }
            TextDrawPlayer.Destroy(player);
            TextDrawGlobal.Hide(player);
            Player.RemoveAV(player);
            if (player.AFK)
                Player.UserAFKs.Remove(new UserAFK() { Player = player });
            player.UpdateData("lastConnection", DateTime.Now);
        }
    }
}
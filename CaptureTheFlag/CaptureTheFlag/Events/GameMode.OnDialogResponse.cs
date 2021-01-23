using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.World;

namespace CaptureTheFlag.Events
{
    public partial class GameMode : BaseMode
    {
        protected override void OnDialogResponse(BasePlayer player, DialogResponseEventArgs e)
        {
            base.OnDialogResponse(player, e);
            player.PlaySound(e.DialogButton == DialogButton.Left ? (1083) : (1084));
        }
    }
}
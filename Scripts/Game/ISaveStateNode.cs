using Godot;

namespace Insanity.Scripts.Game
{
    public interface ISaveStateNode
    {
        Godot.Collections.Dictionary<string, Variant> CaptureSaveState();
        void RestoreSaveState(Godot.Collections.Dictionary<string, Variant> state);
    }
}

using Godot;

namespace Insanity.Scripts.Saving.Options
{
    // Kleiner Singleton für Einstellungen, wie Controller Deadzone, Bildschirmauflösung und dgl.
    // Häufig abgefragte Felder bekommen hier eine direkte Helfermethode
    public static class Options
    {
        [Export] public static string OptionsPath = "user://";
        [Export] public static string OptionsFile = "game_options.sav";
        
        public static ConfigFile CFile = new ConfigFile();
        
        
        public static Error SaveField(string section, string key, Variant value)
        {
            Error loadError = CFile.Load(OptionsPath + OptionsFile);

            if (loadError != Error.Ok) {return loadError;}
            
            CFile.SetValue(section, key, value);

            return CFile.Save(OptionsPath + OptionsFile);
        }

        
        public static Variant LoadField(string section, string key)
        {
            Error loadError = CFile.Load(OptionsPath + OptionsFile);

            if (loadError != Error.Ok) {return default;}

            return CFile.GetValue(section, key);
        }
    }
}


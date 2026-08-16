namespace Northbound.Endings
{
    public static class EndingDialogueMap
    {
        public static readonly string[] SupportedVariantIds = { "elias_ready", "elias_remember", "home_garage", "home_bus_stop", "no_map_photo", "no_map_notebook", "no_map_house_key", "no_map_map", "pause_journey" };
        public static string DialogueId(string variant) => variant switch
        {
            "elias_ready" => "ending_northbound_high", "elias_remember" => "ending_northbound_low",
            "home_garage" => "ending_home_high", "home_bus_stop" => "ending_home_low",
            "no_map_notebook" => "ending_no_map_notebook", "no_map_map" => "ending_no_map_map",
            "no_map_photo" => "ending_no_map_photo", "no_map_house_key" => "ending_no_map_house_key",
            "pause_journey" => "ending_pause_journey",
            "maya_mural" => "ending_maya", "noah_radio" => "ending_noah", "leo_diner" => "ending_leo", _ => null
        };
    }
}

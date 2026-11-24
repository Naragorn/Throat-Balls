namespace ThroatBalls.Logic
{
    /// <summary>
    /// What the Mod Menu shows for enum values. The game shows an enum's raw names ("BluntOnly"); a value list with
    /// titles reads better. The menu saves the index, so renaming a title keeps the player's choice.
    /// </summary>
    public static class OptionTitles
    {
        /// <summary>Blunt includes fists and kicks: the game deals them as Blunt damage (Nadi asked, 2026-11-24).</summary>
        public static string Of(AllowedHits value)
        {
            switch (value)
            {
                case AllowedHits.FistsAndKicksOnly:
                    return "Fists And Kicks Only";
                case AllowedHits.AnyHit:
                    return "Any Hit, Blades Too";
                default:
                    return "Fists, Kicks, Blunt Weapons";
            }
        }
    }
}

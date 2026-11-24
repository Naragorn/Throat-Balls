using System;
using System.Linq;
using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    public class OptionTitlesTests
    {
        [Fact]
        public void Allowed_hits_titles_say_that_fists_and_kicks_count_as_blunt()
        {
            Assert.Equal("Fists, Kicks, Blunt Weapons", OptionTitles.Of(AllowedHits.BluntOnly));
            Assert.Equal("Fists And Kicks Only", OptionTitles.Of(AllowedHits.FistsAndKicksOnly));
            Assert.Equal("Any Hit, Blades Too", OptionTitles.Of(AllowedHits.AnyHit));
        }

        [Fact]
        public void Every_allowed_hits_value_has_its_own_title()
        {
            var values = (AllowedHits[])Enum.GetValues(typeof(AllowedHits));
            Assert.Equal(AllowedHits.BluntOnly, values[0]); // the menu falls back to the first entry
            Assert.Equal(values.Length, Array.ConvertAll(values, OptionTitles.Of).Distinct().Count());
        }
    }
}

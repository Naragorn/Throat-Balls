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

        [Fact]
        public void Throat_sound_titles_number_the_clips_as_they_were_picked()
        {
            Assert.Equal("Random", OptionTitles.Of(ThroatSound.Random));
            Assert.Equal("Sound 1", OptionTitles.Of(ThroatSound.Sound1));
            Assert.Equal("Sound 2", OptionTitles.Of(ThroatSound.Sound2));
            Assert.Equal("Sound 3", OptionTitles.Of(ThroatSound.Sound3));
            Assert.Equal("Sound 4", OptionTitles.Of(ThroatSound.Sound4));
        }

        [Fact]
        public void Every_throat_sound_value_has_its_own_title()
        {
            var values = (ThroatSound[])Enum.GetValues(typeof(ThroatSound));
            Assert.Equal(values.Length, Array.ConvertAll(values, OptionTitles.Of).Distinct().Count());
        }
    }
}

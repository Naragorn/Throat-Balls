using System.Collections.Generic;
using System.Linq;
using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    public class OptionSpecTests
    {
        public static IEnumerable<object[]> AllSpecs()
        {
            return OptionSpecs.All.Select(spec => new object[] { spec.Name, spec });
        }

        [Theory]
        [MemberData(nameof(AllSpecs))]
        public void Default_is_one_of_the_values_exactly(string name, FloatOptionSpec spec)
        {
            // The game's mod menu finds the default with object.Equals on the
            // boxed float, so the default must be bit-identical to one entry.
            Assert.Contains(spec.Values(), v => v.Equals(spec.Default));
            Assert.False(string.IsNullOrEmpty(name));
        }

        [Theory]
        [MemberData(nameof(AllSpecs))]
        public void Values_run_from_min_to_max_in_steps(string name, FloatOptionSpec spec)
        {
            var values = spec.Values();

            Assert.Equal(spec.Min, values.First());
            Assert.Equal(spec.Max, values.Last(), 4);
            for (var i = 1; i < values.Length; i++)
            {
                Assert.Equal(spec.Step, values[i] - values[i - 1], 4);
            }
            Assert.False(string.IsNullOrEmpty(name));
        }

        [Fact]
        public void Option_names_are_unique()
        {
            // The game saves options by name per mod.
            var names = OptionSpecs.All.Select(s => s.Name).ToList();
            Assert.Equal(names.Count, names.Distinct().Count());
        }

        [Fact]
        public void Enum_defaults_are_the_first_entries()
        {
            // The mod menu cannot match an enum default and falls back to the first entry.
            Assert.Equal(0, (int)AllowedHits.BluntOnly);
            Assert.Equal(0, (int)ComboOrder.EitherOrder);
            Assert.Equal(0, (int)FallStyle.CurlUp);
            Assert.Equal(0, (int)DropWeapons.WhenTheyGoDown);
        }

        [Fact]
        public void Labels_show_units()
        {
            Assert.Equal("7 cm", OptionSpecs.ThroatZoneSize.Label(0.07f));
            Assert.Equal("1.5 m/s", OptionSpecs.MinHitSpeed.Label(1.5f));
            Assert.Equal("4 s", OptionSpecs.ComboWindow.Label(4f));
            Assert.Equal("-8 cm", OptionSpecs.GroinHeightOffset.Label(-0.08f));
            Assert.Equal("x0.6", OptionSpecs.GroinCrouchHeight.Label(0.6f));
        }
    }
}

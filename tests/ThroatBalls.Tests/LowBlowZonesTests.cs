using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    public class LowBlowZonesTests
    {
        [Fact]
        public void Throat_sits_in_front_of_and_above_the_neck()
        {
            var settings = LowBlowClassifierTests.Settings();

            var center = LowBlowZones.ThroatCenter(new Vec3(1f, 1.5f, 2f), new Vec3(0f, 0f, 1f), settings);

            Assert.Equal(1f, center.X, 4);
            Assert.Equal(1.55f, center.Y, 4);
            Assert.Equal(2.05f, center.Z, 4);
        }

        [Fact]
        public void Groin_sits_in_front_of_and_below_the_hips()
        {
            var settings = LowBlowClassifierTests.Settings();

            var center = LowBlowZones.GroinCenter(new Vec3(0f, 1f, 0f), new Vec3(1f, 0f, 0f), settings);

            Assert.Equal(0.08f, center.X, 4);
            Assert.Equal(0.92f, center.Y, 4);
            Assert.Equal(0f, center.Z, 4);
        }

        [Fact]
        public void A_tilted_facing_only_counts_horizontally()
        {
            var settings = LowBlowClassifierTests.Settings();

            // Facing forward and 45 degrees down: the zone still sits straight ahead, at its own height.
            var center = LowBlowZones.ThroatCenter(Vec3.Zero, new Vec3(0f, -1f, 1f), settings);

            Assert.Equal(0f, center.X, 4);
            Assert.Equal(0.05f, center.Y, 4);
            Assert.Equal(0.05f, center.Z, 4);
        }

        [Fact]
        public void Facing_straight_up_has_no_forward_and_keeps_only_the_height()
        {
            var settings = LowBlowClassifierTests.Settings();

            var center = LowBlowZones.GroinCenter(Vec3.Zero, Vec3.Up, settings);

            Assert.Equal(0f, center.X, 4);
            Assert.Equal(-0.08f, center.Y, 4);
            Assert.Equal(0f, center.Z, 4);
        }
    }
}

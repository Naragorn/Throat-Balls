using System;
using System.Collections.Generic;
using System.Globalization;

namespace ThroatBalls.Logic
{
    public enum OptionUnit
    {
        /// <summary>Stored in meters, shown in centimeters.</summary>
        Centimeters,
        Degrees,
        Seconds,
        Meters,
        Multiplier,

        /// <summary>Stored as 0..1, shown in percent.</summary>
        Percent,

        /// <summary>A bare number.</summary>
        Plain,

        /// <summary>A speed in meters per second.</summary>
        MetersPerSecond,
    }

    /// <summary>
    /// Range, step and default of a numeric Mod Menu option. Values are generated
    /// with decimal arithmetic so the default is bit-identical to one entry: the
    /// game's mod menu looks the default up with object.Equals on the boxed float.
    /// </summary>
    public sealed class FloatOptionSpec
    {
        public FloatOptionSpec(string name, float min, float max, float step, float defaultValue, OptionUnit unit)
        {
            Name = name;
            Min = min;
            Max = max;
            Step = step;
            Default = defaultValue;
            Unit = unit;
        }

        public string Name { get; }
        public float Min { get; }
        public float Max { get; }
        public float Step { get; }
        public float Default { get; }
        public OptionUnit Unit { get; }

        public float[] Values()
        {
            var min = (decimal)Min;
            var step = (decimal)Step;
            var count = (int)Math.Round(((decimal)Max - min) / step) + 1;
            var values = new float[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = (float)(min + i * step);
            }
            return values;
        }

        public string Label(float value)
        {
            var culture = CultureInfo.InvariantCulture;
            switch (Unit)
            {
                case OptionUnit.Centimeters:
                    return ((decimal)value * 100m).ToString("0.#", culture) + " cm";
                case OptionUnit.Degrees:
                    return ((decimal)value).ToString("0.#", culture) + "°";
                case OptionUnit.Meters:
                    return ((decimal)value).ToString("0.0#", culture) + " m";
                case OptionUnit.Multiplier:
                    return "x" + ((decimal)value).ToString("0.0#", culture);
                case OptionUnit.Percent:
                    return ((decimal)value * 100m).ToString("0.#", culture) + " %";
                case OptionUnit.Plain:
                    return ((decimal)value).ToString("0.0#", culture);
                case OptionUnit.MetersPerSecond:
                    return ((decimal)value).ToString("0.0#", culture) + " m/s";
                default:
                    return ((decimal)value).ToString("0.#", culture) + " s";
            }
        }
    }
}

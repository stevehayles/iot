// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using System.Device;
using System.Device.Spi;
using System.Device.I2c;
using System.Device.Pwm;

using Iot.Device.Adc;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using Iot.Units;

using Xunit;

using static System.Device.Gpio.Tests.SetupHelpers;

namespace System.Device.Gpio.Tests
{
    public class ProtocolsTests
    {
        [Fact]
        public void SPI_Mcp3008CanRead()
        {
            using (Mcp3008 adc = CreateAdc())
            {
                // We don't care about specific value for the first 5 channels
                for (int i = 0; i <= 4; i++)
                {
                    Assert.InRange(adc.Read(i), MinAdc, MaxAdc);
                }

                // Voltage divider with equal resistors (50% VCC)
                AdcValueAround(HalfAdc, adc.Read(5));

                // GND
                Assert.InRange(adc.Read(6), MinAdc, MinAdc + 5);

                // VCC
                Assert.InRange(adc.Read(7), MaxAdc - 5, MaxAdc);
            }
        }

        [Fact]
        public void I2C_Bme280CanRead()
        {
            using (Bme280 bme280 = CreateBme280())
            {
                Assert.True(bme280.TryReadTemperature(out Temperature temperature));

                // assuming that tests are run in the room temperature
                // worst case scenario: it's very hot outside
                Assert.InRange(temperature.Celsius, 15, 40);

                Assert.True(bme280.TryReadPressure(out double pressurePa));
                // https://en.wikipedia.org/wiki/List_of_weather_records
                // Min and max are extremes recorded on land
                double pressureHPa = pressurePa / 100.0;
                Assert.InRange(pressureHPa, 892, 1084);

                Assert.True(bme280.TryReadHumidity(out double relativeHumidity));
                Assert.InRange(relativeHumidity, 0, 100);
            }
        }

        [Fact]
        public void PWM_DutyCycleIsSetCorrectly()
        {
            using (PwmChannel pwm = CreatePwmChannel())
            using (Mcp3008 adc = CreateAdc())
            {
                for (int n = 0; n < 2; n++)
                {
                    for (int i = 0; i <= 10; i++)
                    {
                        pwm.DutyCycle = i * 0.1;

                        // Settling time is ~1.1ms (when going from GND to max)
                        // R=4.7k ohm
                        // C=0.1uF
                        // f=10k Hz
                        // peak to peak is ~0.18V (5.5% VCC)
                        //   in this scenario is 2 * error
                        Thread.Sleep(2);

                        int expected = (int)Math.Round(pwm.DutyCycle * 1023.0);
                        AdcValueAround(expected, adc.Read(0));
                    }
                }
            }
        }
    }
}

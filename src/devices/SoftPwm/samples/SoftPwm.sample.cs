// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Pwm.Drivers;
using System.Threading;

class Program
{

    static void Main(string[] args)
    {
        Console.WriteLine("Hello PWM!");

        using (var pwmChannel = new SoftwarePwmChannel(17, 200, 0))
        {
            pwmChannel.Start();
            for (double fill = 0.0; fill <= 1.0; fill += 0.01)
            {
                pwmChannel.DutyCyclePercentage = fill;
                Thread.Sleep(500);
            }
        }
    }
}

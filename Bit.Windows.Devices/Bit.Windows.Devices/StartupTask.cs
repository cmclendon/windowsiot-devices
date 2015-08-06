using System;
using System.Collections.Generic;
using Bit.Windows.Devices.I2c.LCD;
using Bit.Windows.Devices.I2c;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace Bit.Windows.Devices
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;
        IBackgroundTaskInstance taskInstance;

        // small display address: 0x3F
        // big display address: 0x27

        private void characterTest()
        {
            HD44780UI2c lcd = new HD44780UI2c(0x27, DATAMODE.MODE_4BIT, DISPLAYDEF.DISPLAY4x20);

            lcd.Initialize();

            lcd.EnableBacklight = true;
            lcd.CursorVisible = true;
            lcd.BlinkingCursor = true;
            lcd.DisplayEnabled = true;

            lcd.WriteLine(0, "Christopher McLendon", TEXT_JUSTIFICATION.CENTER);
            lcd.WriteLine(3, "http://BIT.expert", TEXT_JUSTIFICATION.CENTER);
            lcd.WriteLine(1, "Microsoft Corp.", TEXT_JUSTIFICATION.CENTER);

        }

        private void probeI2c()
        {
            List<byte> deviceAddresses = I2cHelper.ProbeI2c();
            foreach (byte addr in deviceAddresses)
                System.Diagnostics.Debug.WriteLine("Found device at {0:X}", addr);
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            this.taskInstance = taskInstance;
            this.deferral = taskInstance.GetDeferral();

            //probeI2c();
            characterTest();
        }
    }
}
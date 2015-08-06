using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.I2c;
using Windows.Devices.Enumeration;

namespace Bit.Windows.Devices.I2c
{
    public class I2cHelper
    {
        static async public Task<I2cDevice> I2cGetDevice(byte addr, I2cBusSpeed speed = I2cBusSpeed.StandardMode)
        {
            I2cDevice device;

            // Initialize our I2c settings; use standard mode
            I2cConnectionSettings settings = new I2cConnectionSettings(addr);
            settings.BusSpeed = speed;

            // Enumerate and select our I2c controller
            string aqs = I2cDevice.GetDeviceSelector();
            var dis = await DeviceInformation.FindAllAsync(aqs);

            // Wait for our servo controller instance
            device = await I2cDevice.FromIdAsync(dis[0].Id, settings);

            return device;
        }
        
        static public void wait(int ms)
        {
            System.Threading.Tasks.Task.Delay(ms).Wait();
        }

        static public List<byte> ProbeI2c(byte startAddr = 0x0, byte stopAddr = 0x7F)
        {
            List<byte> addressList = new List<byte>();
            I2cTransferResult result;
            byte[] data = new byte[] { 0x0 };

            if ((stopAddr < startAddr) || (startAddr < 0x00) || (stopAddr > 0x7F))
                throw new InvalidOperationException("I2c address must be a valid 7-bit address");
            
            for (byte addr = startAddr; addr <= stopAddr; addr++)
            {
                // wait on the i2cdevice
                Task<I2cDevice> task = I2cGetDevice(addr);
                task.Wait();

                // attempt to write a 0x0 byte to the device
                result = task.Result.WritePartial(data);
                if (result.Status != I2cTransferStatus.SlaveAddressNotAcknowledged) addressList.Add(addr);
            }

            return addressList;  
        }
    }
}

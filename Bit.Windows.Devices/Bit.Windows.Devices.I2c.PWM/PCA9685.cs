using System;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Bit.Windows.Devices.Diagnostics;
using Bit.Windows.Devices.I2c;

namespace Bit.Windows.Devices.I2c.PWM
{
    public class PCA9685
    {
        //public const UInt16 SERVOMIN = 200;
        //public const UInt16 SERVOMAX = 500;

        public const UInt16 SERVOMIN = 300;
        public const UInt16 SERVOMAX = 350;


        private byte PCA9685_SUBADR1 = 0x2;
        private byte PCA9685_SUBADR2 = 0x3;
        private byte PCA9685_SUBADR3 = 0x4;

        private byte PCA9685_MODE1 = 0x00;
        private byte PCA9685_MODE2 = 0x01;
        private byte PCA9685_PRESCALE = 0xFE;

        private UInt16 LED0_ON_L = 0x6;
        private UInt16 LED0_ON_H = 0x7;
        private UInt16 LED0_OFF_L = 0x8;
        private UInt16 LED0_OFF_H = 0x9;

        private UInt16 ALLLED_ON_L = 0xFA;
        private UInt16 ALLLED_ON_H = 0xFB;
        private UInt16 ALLLED_OFF_L = 0xFC;
        private UInt16 ALLLED_OFF_H = 0xFD;

        /// <summary>
        /// Our I2c buss address
        /// </summary>
        private byte I2caddr;

        /// <summary>
        /// Our single controller instance
        /// </summary>
        I2cDevice device;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="addr">Default bus address is 0x40</param>
        public PCA9685(byte addr = 0x40)
        {
            this.I2caddr = addr;
        }

        async public void begin()
        {
            try
            {
                device = await I2cHelper.I2cGetDevice(this.I2caddr, I2cBusSpeed.FastMode);
                
                // Initialize MODE1 register:
                //      ALLCALL: does not respond to LED All Call
                //      SUB3: does not respond to SUB3 address
                //      SUB2: does not respond to SUB2 address
                //      SUB1: does not respond to SUB1 address
                //      SLEEP: Normal mode
                //      AI: register auto-increment disabled
                //      EXTCLK: Use internal clock
                //      RESTART: disabled
                writeRegister(PCA9685_MODE1, 0x0);

                // Initialize MODE2 register:
                //  Need to make sure that outputs are configured with a totem pole structure
                writeRegister(PCA9685_MODE2, 0x4);

                I2cHelper.wait(0);
            }
            catch (System.Exception e)
            {
                // failed

                Debug.Write("PCA9685.begin", e);
            }
        }

        public void setPWMFreq(double freq)
        {
            // Formula provided by PCA9685 documentation:
            // prescale value = round( osc_clock / (4096 x update_rate)) -1
            double prescaleval = Math.Round(25000000 / (4096 * freq)) - 1;

            // take floor value
            byte prescale = (byte)Math.Round(prescaleval);

            // read the current mode 1 register
            byte oldmode = readRegister(PCA9685_MODE1);

            // sleep low power mode
            byte newmode = (byte) ((oldmode & 0x7F) | 0x10);

            // must set SLEEP bit of MODE1 register to logic 1 in order to set the pre-scaler
            writeRegister(PCA9685_MODE1, newmode);
            writeRegister(PCA9685_PRESCALE, prescale);

            // take the MODE1 register out of sleep mode
            writeRegister(PCA9685_MODE1, oldmode);

            // need to wait 5uS
            I2cHelper.wait(5);

            for (int i = 0; i < 50000000; i++) ;
            writeRegister(PCA9685_MODE1, (byte)(oldmode | 0x80));
        }

        public void setPWM(byte num, UInt16 on, UInt16 off)
        {
            writeRegister((byte)(LED0_ON_L + 4 * num), (byte)(on & 0xFF));
            writeRegister((byte)(LED0_ON_H + 4 * num), (byte)(on >> 8));
            writeRegister((byte)(LED0_OFF_L + 4 * num), (byte)(off & 0xFF));
            writeRegister((byte)(LED0_OFF_H + 4 * num), (byte)(off >> 8));
        }

        public I2cTransferResult writeRegister(byte register, byte data)
        {
            I2cTransferResult result = device.WritePartial(new byte[] { register, data });
            return result;
        }

        public void writeData(byte data)
        {
            I2cTransferResult result = device.WritePartial(new byte[] { data });
        }

        public byte readRegister(byte register)
        {
            byte[] writeData = new byte[] { register };
            byte[] readData = new byte[1];

            while (device == null) ;

            I2cTransferResult result = device.WriteReadPartial(writeData, readData);
            return readData[0];
        }
    }


}

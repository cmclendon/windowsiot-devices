//
// The MIT License(MIT)
//
// Copyright(c) 2015 Christopher McLendon (http://www.bitunify.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// <Description>
//   Base class for communicating with Hitachi HD44780U compatible LCD displays.  This display
//   uses an 8-bit or 4-bit data interface with 3 additional control lines (RS, RW and E).
// </Description>  
//

using System;
using Bit.Windows.Devices.I2c.Expansion;

namespace Bit.Windows.Devices.I2c.LCD
{
    public class HD44780UI2c : HD44780U
    {
        /// <summary>
        /// MPU pin assiggments
        /// </summary>
        private byte pin_Rs;        // register select
        private byte pin_Rw;        // read-write
        private byte pin_En;        // enable

        /// <summary>
        /// Other feature pins
        /// </summary>
        private byte pin_Bl;        // backlight

        /// <summary>
        /// Data pin mappings
        /// </summary>
        private byte[] dataPinMap;

        /// <summary>
        /// I2c Bus Address
        /// </summary>
        private byte i2caddr;

        /// <summary>
        /// PCF8574 8-bit I/O expander
        /// </summary>
        private PCF8574 pcf;

        public HD44780UI2c(byte i2caddr, DATAMODE dataMode, DISPLAYDEF displayMode, 
            byte Rs = 0, byte Rw = 1, byte En = 2, byte Bl = 3, byte D4 = 4, byte D5 = 5, byte D6 = 6, byte D7 = 7)
            : base(DATAMODE.MODE_4BIT, displayMode)
        {
            this.i2caddr = i2caddr;

            // set mask for data pins (4-bit data mode)
            dataPinMap = new byte[4];

            dataPinMap[0] = (byte)(1 << D4);
            dataPinMap[1] = (byte)(1 << D5);
            dataPinMap[2] = (byte)(1 << D6);
            dataPinMap[3] = (byte)(1 << D7);

            // set mask for the MPU interface pins and features
            this.pin_Rs = (byte)(1 << Rs);
            this.pin_Rw = (byte)(1 << Rw);
            this.pin_En = (byte)(1 << En);
            this.pin_Bl = (byte)(1 << Bl);
        }

        private void pulse(byte data)
        {
            byte mask = (byte)(data | pin_En);
            if (EnableBacklight == true) mask |= pin_Bl;

            pcf.Write(mask);
            pcf.Write((byte)(mask & ~pin_En));
        }

        public override void Initialize()
        {
            // initialize our simplified PCF8574 controller
            pcf = new PCF8574();
            pcf.Initialize(i2caddr);

            base.Initialize();
        }

        protected override void SendCommand(Command cmd)
        {
            byte[] message;
            byte data = cmd.Data;

            // configure MPU mask
            byte mpuMask = (byte)
                (((cmd.Rs == true) ? pin_Rs : 0) |
                ((cmd.Rw == true) ? pin_Rw : 0));

            if (dataBusInitialized == true)
            {
                message = new byte[] { 0x0, 0x0 };


                // low nibble
                for (int i = 0; i < 4; i++)
                {
                    if ((data & 0x1) == 1)
                    {
                        message[1] |= dataPinMap[i];
                    }

                    data = (byte)(data >> 1);
                }

                // high nibble
                for (int i = 0; i < 4; i++)
                {
                    if ((data & 0x1) == 1)
                    {
                        message[0] |= dataPinMap[i];
                    }

                    data = (byte)(data >> 1);
                }
            }
            else
            {
                // still writing 4-bits (high nibble only)
                message = new byte[] { 0x0 };
                data = (byte)(data >> 4);

                for (int i = 0; i < 4; i++)
                {
                    if ((data & 0x1) == 1)
                    {
                        message[0] |= dataPinMap[i];
                    }

                    data = (byte)(data >> 1);
                }
            }

            foreach (byte b in message)
            {
                byte register = (byte)(b | mpuMask);
                pulse(register);
            }
        }

        protected override byte Read()
        {
            byte register = 0x0;
            pcf.Read(ref register);
            return register;
        }

        protected override void FeatureStateChanged(FEATURE mask)
        {
            if ((mask & FEATURE.BACKLIGHT) == FEATURE.BACKLIGHT)
            {
                if (EnableBacklight == true) pcf.Write(pin_Bl);
            }
        }
    }
}

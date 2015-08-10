using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bit.Windows.Devices.I2c;
using Windows.Devices.I2c;

namespace Bit.Windows.Devices.I2c.Sensor
{
    public class MPL3115A2
    {
        private I2cDevice device;

        private enum REGISTER_ADDRESS_MAP : byte
        {
            STATUS = 0X00,
            OUT_P_MSB = 0X01,
            OUT_P_CSB = 0X02,
            OUT_P_LSB = 0X03,
            OUT_T_MSB = 0X04,
            OUT_T_LSB = 0X05,
            DR_STATUS = 0X06,
            OUT_P_DELTA_MSB = 0X07,
            OUT_P_DELTA_CSB = 0X08,
            OUT_P_DELTA_LSB = 0X09,
            OUT_T_DELTA_MSB = 0X0A,
            OUT_T_DELTA_LSB = 0X0B,
            WHO_AM_I = 0X0C,
            F_STATUS = 0X0D,
            F_DATA = 0X0E,
            F_SETUP = 0X0F,
            TIME_DLY = 0X10,
            SYSMOD = 0X11,
            INT_SOURCE = 0X12,
            PT_DATA_CFG = 0X13,
            BAR_IN_MSB = 0X14,
            BAR_IN_LSB = 0X15,
            P_TGT_MSB = 0X16,
            P_TGT_LSB = 0X17,
            T_TGT = 0X18,
            P_WND_MSB = 0X19,
            P_WND_LSB = 0X1A,
            T_WND = 0X1B,
            P_MIN_MSB = 0X1C,
            P_MIN_CSB = 0X1D,
            P_MIN_LSB = 0X1E,
            T_MIN_MSB = 0X1F,
            T_MIN_LSB = 0X20,
            P_MAX_MSB = 0X21,
            P_MAX_CSB = 0X22,
            P_MAX_LSB = 0X23,
            T_MAX_MSB = 0X24,
            T_MAX_LSB = 0X25,
            CTRL_REG1 = 0X26,
            CTRL_REG2 = 0X27,
            CTRL_REG3 = 0X28,
            CTRL_REG4 = 0X29,
            CTRL_REG5 = 0X2A,
            OFF_P = 0X2B,
            OFF_T = 0X2C,
            OFF_H = 0X2D
        };

               

        public MPL3115A2()
        {

        }

        public void Initialize(byte address)
        {
            Task<I2cDevice> task = I2cHelper.I2cGetDevice(address);
            task.Wait();
            device = task.Result;
        }
    }
}

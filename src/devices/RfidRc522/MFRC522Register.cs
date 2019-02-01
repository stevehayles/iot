using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.RfidRc522
{
    // https://www.nxp.com/docs/en/data-sheet/MFRC522.pdf
    // 9.2, Table 20.
    public enum MFRC522Register : byte
    {
        // Command and status
        CommandReg = 0x01,
        ComlEnReg = 0x02,
        DivlEnReg = 0x03,
        ComIrqReg = 0x04,
        DivIrqReg = 0x05,
        ErrorReg = 0x06,
        Status1Reg = 0x07,
        Status2Reg = 0x08,
        FIFODataReg = 0x09,
        FIFOLevelReg = 0x0A,
        WaterLevelReg = 0x0B,
        ControlReg = 0x0C,
        BitFramingReg = 0x0D,
        CollReg = 0x0E,

        // Command
        ModeReg = 0x11,
        TxModeReg = 0x12,
        RxModeReg = 0x13,
        TxControlReg = 0x14,
        TxASKReg = 0x15,
        TxSelReg = 0x16,
        RxSelReg = 0x17,
        RxThresholdReg = 0x18,
        DemodReg = 0x19,
        MfTxReg = 0x1C,
        MfRxReg = 0x1D,
        SerialSpeedReg = 0x1F,

        // Configuration
        CRCResultRegHigh = 0x21,
        CRCResultRegLow = 0x22,
        ModWidthReg = 0x24,
        RFCfgReg = 0x26,
        GsNReg = 0x27,
        CWGsPReg = 0x28,
        ModGsPReg = 0x29,
        TModeReg = 0x2A,
        TPrescalerReg = 0x2B,
        TReloadRegHigh = 0x2C, // TODO: ALL HIGH/LOW might be swapped
        TReloadRegLow = 0x2D,
        TCounterValRegHigh = 0x2E,
        TCounterValRegLow = 0x2F,

        // Test
        TestSel1Reg = 0x31,
        TestSel2Reg = 0x32,
        TestPinEnReg = 0x33,
        TestPinValueReg = 0x34,
        TestBusReg = 0x35,
        AutoTestReg = 0x36,
        VersionReg = 0x37,
        AnalogTestReg = 0x38,
        TestDAC1Reg = 0x39,
        TestDAC2Reg = 0x3A,
        TestADCReg = 0x3B,
    }
}

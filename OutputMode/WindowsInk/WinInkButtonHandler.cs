using HidSharp;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using VoiDPlugins.Library.VMulti;
using VoiDPlugins.Library.VMulti.Device;

namespace VoiDPlugins.OutputMode
{
    [PluginName("Windows Ink")]
    public class WinInkButtonHandler : ButtonHandler, IBinding
    {
        public static string[] ValidButtons { get; } = new string[]
        {
            "Pen Tip",
            "Pen Button",
            "Eraser (Toggle)",
            "Eraser (Hold)"
        };

        [Property("Button"), PropertyValidated(nameof(ValidButtons))]
        public string Button { get; set; }

        private enum ButtonBits : int
        {
            Press = 1,
            Barrel = 2,
            Eraser = 4,
            Invert = 8,
            InRange = 16
        }

        private static bool EraserState;
        private static HidStream Device;
        private static new DigitizerInputReport Report;

        public void Press(IDeviceReport report)
        {
            switch (Button)
            {
                case "Pen Tip":
                    EnableBit((int)(EraserState ? ButtonBits.Eraser : ButtonBits.Press));
                    break;

                case "Pen Button":
                    EnableBit((int)ButtonBits.Barrel);
                    break;

                case "Eraser (Toggle)":
                    if (EraserState)
                    {
                        DisableBit((int)ButtonBits.Invert);
                        EraserState = false;
                        EraserStateTransition();
                    }
                    else
                    {
                        // EnableBit((int)Button.Invert);
                        DisableBit((int)ButtonBits.Press);
                        EraserState = true;
                        EraserStateTransition();
                    }
                    break;

                case "Eraser (Hold)":
                    // EnableBit((int)Button.Invert);
                    DisableBit((int)ButtonBits.Press);
                    EraserState = true;
                    EraserStateTransition();
                    break;
            }
        }

        public void Release(IDeviceReport report)
        {
            switch (Button)
            {
                case "Pen Tip":
                    DisableBit((int)ButtonBits.Press);
                    DisableBit((int)ButtonBits.Eraser);
                    break;

                case "Pen Button":
                    DisableBit((int)ButtonBits.Barrel);
                    break;

                case "Eraser (Hold)":
                    // DisableBit((int)Button.Invert);
                    EraserState = false;
                    EraserStateTransition();
                    break;

            }
        }

        private void EraserStateTransition()
        {
            var buttons = Report.Buttons;
            var pressure = Report.Pressure;

            // Send In-Range but no tips
            DisableBit((int)ButtonBits.Press);
            DisableBit((int)ButtonBits.Eraser);
            Report.Pressure = 0;
            Device.Write(Report.ToBytes());

            // Send Out-Of-Range
            Report.Buttons = 0;
            Device.Write(Report.ToBytes());

            // Send In-Range but no tips
            EnableBit((int)ButtonBits.InRange);
            if (EraserState)
                EnableBit((int)ButtonBits.Invert);

            Device.Write(Report.ToBytes());

            // Send Proper Report
            Report.Buttons = buttons;
            Report.Pressure = pressure;
        }

        public static void SetReport(DigitizerInputReport Report)
        {
            ButtonHandler.SetReport(Report);
            WinInkButtonHandler.Report = Report;
            EnableBit((int)ButtonBits.InRange);
        }

        public static void SetDevice(HidStream device)
        {
            Device = device;
        }
    }
}
using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using A6281;

namespace Telegraph {

    public class Program {
        public static void Main() {
            Receiver r = new Receiver();
            r.SetTransmissionHandler(new ShiftBriteHandler());
            r.ConnectToTransmitter("localhost", 9091);
            r.ListenForTransmissions();
        }
    }

    public class ShiftBriteHandler : TransmissionHandler {
        A6281.Single shiftbrite;

        public ShiftBriteHandler() {
            shiftbrite = new A6281.Single(Pins.GPIO_NONE, Pins.GPIO_PIN_D9, Pins.GPIO_PIN_D10, SPI.SPI_module.SPI1);
            shiftbrite.On = false;
        }

        public struct Color {
            public ushort red, green, blue;
        }

        public void Handle(String msg) {
            Debug.Print(msg);
            Color color = StringToColor(msg);
            ShowColor(color);
        }

        public Color StringToColor(String str) {
            char[] chars = str.ToCharArray();
            uint[] sum = new uint[3];
            int count = chars.Length;

            for (uint i = 0; i < count; i++) {
                sum[i % 3] += (uint)chars[i] * i;
            }

            Color color = new Color();
            color.red = (ushort)(sum[0] % 1024);
            color.green = (ushort)(sum[1] % 1024);
            color.blue = (ushort)(sum[2] % 1024);
            return color;
        }

        public void ShowColor(Color color) {
            shiftbrite.On = true;
            shiftbrite.SetColorImmediate(color.red, color.green, color.blue);
            Thread.Sleep(250);
            shiftbrite.On = false;
        }
    }
}

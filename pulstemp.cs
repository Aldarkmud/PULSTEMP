using System;
using System.Collections;
using System.Device.Spi;
using System.Diagnostics;
using System.IO;
using System.Threading;
using nanoFramework.Hardware.Esp32;
using nanoFramework.System.IO.FileSystem;

namespace PULSTEMP
{
    public class Program
    {
        private static SDCard sdCard;
        private static readonly byte[] numbers = new byte[11];
        private static SpiDevice spiDeviceSensor;
        private static SpiDevice spiDeviceDisplay;

        public static void Main()
        {
            byte temp0 = (byte)0b10000000 | (byte)0b10000000;
            byte temp1 = (byte)0b00000001 | (byte)0b00011000 | (byte)0b00100000 | (byte)0b00100000 | (byte)0b10000000 | (byte)0b10000000;
            byte temp2 = (byte)0b00000000 | (byte)0b00000000 | (byte)0b10000000;
            byte temp3 = (byte)0b00000001 | (byte)0b00000000 | (byte)0b10000000;
            byte temp4 = (byte)0b00000001 | (byte)0b00000000 | (byte)0b10000000 | (byte)0b10000000;
            byte temp5 = (byte)0b00000010 | (byte)0b00000000 | (byte)0b10000000;
            byte temp6 = (byte)0b00000010 | (byte)0b00000000;
            byte temp7 = (byte)0b00000000 | (byte)0b00000000 | (byte)0b00000000 | (byte)0b00100000 | (byte)0b10000000 | (byte)0b10000000;
            byte temp8 = (byte)0x00 | (byte)0b10000000;
            byte temp9 = (byte)0b00000000 | (byte)0b10000000;
            byte temp10 = (byte)0b00000010 | (byte)0b00000000 | (byte)0b10000000 | (byte)0b10000000;

            numbers[0] = temp0;
            numbers[1] = temp1;
            numbers[2] = temp2;
            numbers[3] = temp3;
            numbers[4] = temp4;
            numbers[5] = temp5;
            numbers[6] = temp6;
            numbers[7] = temp7;
            numbers[8] = temp8;
            numbers[9] = temp9;
            numbers[10] = temp10; // Celsius

            Debug.WriteLine("Starting PULSTEMP...");

            // SPI GPIO configuration for MAX6675 and SN74HC595
            Configuration.SetPinFunction(23, DeviceFunction.SPI1_MOSI);
            Configuration.SetPinFunction(25, DeviceFunction.SPI1_MISO);
            Configuration.SetPinFunction(19, DeviceFunction.SPI1_CLOCK);
            int chipSelectPinSensor = 5;   // CS for MAX6675
            // CS 21 for SN74HC595

            // SPI GPIO configuration for SD card
            Configuration.SetPinFunction(15, DeviceFunction.SPI2_MOSI); // MOSI for SD card (CMD)
            Configuration.SetPinFunction(2,  DeviceFunction.SPI2_MISO); // MISO for SD card (DAT0)
            Configuration.SetPinFunction(14, DeviceFunction.SPI2_CLOCK); // SCLK for SD card (CLK)
            int chipSelectPinSD = 13; // CS for SD card (RCS)

            // SPI setup for MAX6675
            var sensorConnectionSettings = new SpiConnectionSettings(1, chipSelectPinSensor)
            {
                ClockFrequency = 2_000_000,
                DataBitLength = 8,
                DataFlow = DataFlow.MsbFirst,
                Mode = SpiMode.Mode0
            };

            // SPI setup for SN74HC595
            var displayConnectionSettings = new SpiConnectionSettings(1, 21)
            {
                ClockFrequency = 4_000_000,
                DataBitLength = 8,
                DataFlow = DataFlow.MsbFirst,
                Mode = SpiMode.Mode0
            };

            // SPI setup for SD card
            var sdConnectionSettings = new SpiConnectionSettings(2, chipSelectPinSD)
            {
                ClockFrequency = 700_000,
                DataBitLength = 8,
                DataFlow = DataFlow.MsbFirst,
                Mode = SpiMode.Mode0
            };

            // Creating an SPI device for MAX6675
            spiDeviceSensor = SpiDevice.Create(sensorConnectionSettings);
            spiDeviceDisplay = SpiDevice.Create(displayConnectionSettings);

            // Initializing MAX6675
            Max6675 sensor = new(spiDeviceSensor);

            // Storage for temperature values
            ArrayList temperatureList = new ArrayList();

            // Heating status flag
            bool isHeating = true;

            // Initializing the SD card
            sdCard = new SDCard(new SDCardSpiParameters { spiBus = 2, chipSelectPin = (uint)chipSelectPinSD });

            // Using events to interact with the SD card
            StorageEventManager.RemovableDeviceInserted += StorageEventManager_RemovableDeviceInserted;
            StorageEventManager.RemovableDeviceRemoved += StorageEventManager_RemovableDeviceRemoved;

            // Attempting to manually mount the SD card
            if (MountSDCard())
            {
                Debug.WriteLine("The SD card has been successfully mounted.");
            }
            else
            {
                Debug.WriteLine("Failed to mount the SD card.");
            }

            // Main loop
            int seconds = 0;
            while (true)
            {
                try
                {
                    // Checking MAX6675 connection
                    if (sensor.IsSensorConnected())
                    {
                        // Reading temperature
                        double temperature = sensor.GetTemperature();
                        seconds++;
                        Console.WriteLine($"Time: {seconds}s, Temp: {temperature:F2} °C");
                        temperatureList.Add(temperature);
                        PrintOnDisplay(temperature);

                        if (isHeating && temperature >= 999)
                        {
                            isHeating = false;
                            Console.WriteLine("Temperature threshold reached (51 °C).");
                        }
                    }
                                        if (isHeating && temperature <= 999)
                    {
                        Console.WriteLine("The temperature has decreased to 50°C. Completion.");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("Communication error with MAX6675.");
                }

                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        // Saving data to the SD card
        SaveTemperatureDataToSD(temperatureList);

        // Unmounting the SD card
        UnmountSDCard();

        // Displaying all temperatures in the console
        Console.WriteLine("\nAll measured temperature values:");
        foreach (double temp in temperatureList)
        {
            Console.WriteLine($"{temp:F2} °C");
        }
    }

    private static void PrintOnDisplay(double ValueToPrint)
    {
        int hundreds = (int)ValueToPrint / 100;
        int tens = (int)(ValueToPrint - (hundreds * 100)) / 10;
        int last = (int)(ValueToPrint - (hundreds * 100) - (tens * 10));

        spiDeviceDisplay.WriteByte(numbers[10]);
        spiDeviceDisplay.WriteByte(numbers[hundreds]);
        spiDeviceDisplay.WriteByte(numbers[tens]);
        spiDeviceDisplay.WriteByte(numbers[last]);
    }

        private static bool MountSDCard()
    {
        try
        {
            Thread.Sleep(3000);
            sdCard.Mount();
            Thread.Sleep(500);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SD card mounting error: {ex.Message}");
            return false;
        }
    }

    private static void UnmountSDCard()
    {
        if (sdCard.IsMounted)
        {
            try
            {
                sdCard.Unmount();
                Debug.WriteLine("The SD card has been successfully unmounted.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SD card unmounting error: {ex.Message}");
            }
        }
    }

    private static void StorageEventManager_RemovableDeviceInserted(object sender, RemovableDeviceEventArgs e)
    {
        Debug.WriteLine($"SD card inserted. Path: {e.Drive}");
    }

    private static void StorageEventManager_RemovableDeviceRemoved(object sender, RemovableDeviceEventArgs e)
    {
        Debug.WriteLine($"SD card removed. Path: {e.Drive}");
    }

        private static void SaveTemperatureDataToSD(ArrayList temperatureList)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string filePath = $"D:/temperature_data_{timestamp}.csv";

        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(fileStream))
            {
                writer.WriteLine("Seconds;Temperatures");

                for (int i = 0; i < temperatureList.Count; i++)
                {
                    int tempRounded = (int)(((double)temperatureList[i]) + 0.5);
                    writer.WriteLine($"{i + 1};{tempRounded}");
                }
            }

            Console.WriteLine($"Data has been written to the file: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Write error: {ex.Message}");
        }
    }

    public class Max6675
    {
        private readonly SpiDevice _spiDevice;

        public Max6675(SpiDevice spiDevice)
        {
            _spiDevice = spiDevice;
        }

                public Max6675(SpiDevice spiDevice)
        {
            _spiDevice = spiDevice;
        }

        public bool IsSensorConnected()
        {
            byte[] buffer = new byte[2];
            _spiDevice.Read(buffer);
            ushort value = (ushort)((buffer[0] << 8) | buffer[1]);
            return value != 0xFFFF;
        }

        public double GetTemperature()
        {
            byte[] buffer = new byte[2];
            _spiDevice.Read(buffer);
            ushort value = (ushort)((buffer[0] << 8) | buffer[1]);

            if ((value & 0x04) != 0)
                throw new Exception("Thermocouple disconnection!");

            value >>= 3;
            return value * 0.25;
        }
    }
# PULSTEMP 

**Temperature monitoring system built on ESP32 using C# and .NET nanoFramework.**

The system reads temperature from a thermocouple sensor, displays it in real time on a 7-segment display, and automatically saves all measurements to an SD card in CSV format.

---

## Features

- Real-time temperature reading via MAX6675 thermocouple sensor
- 4-digit 7-segment display output driven by SN74HC595 shift register
- Automatic data logging to SD card in CSV format (timestamped files)
- Three simultaneous SPI devices on ESP32
- Sensor disconnection detection and error handling
- Hot-swap SD card support via storage event manager

---

## Hardware

| Component | Description |
|---|---|
| ESP32 | Main microcontroller |
| MAX6675 | Thermocouple-to-SPI converter |
| Type-K Thermocouple | High-temperature probe |
| SN74HC595 | 8-bit shift register for display |
| 4-digit 7-segment display | Temperature output |
| SD card module | Data logging |

---

## Wiring

| ESP32 Pin | Connected To |
|---|---|
| GPIO 23 | SPI1 MOSI (MAX6675 + SN74HC595) |
| GPIO 25 | SPI1 MISO (MAX6675) |
| GPIO 19 | SPI1 CLOCK (MAX6675 + SN74HC595) |
| GPIO 5 | CS — MAX6675 |
| GPIO 21 | CS — SN74HC595 |
| GPIO 15 | SPI2 MOSI (SD card) |
| GPIO 2 | SPI2 MISO (SD card) |
| GPIO 14 | SPI2 CLOCK (SD card) |
| GPIO 13 | CS — SD card |

---

## Tech Stack

- **Language:** C# (.NET nanoFramework)
- **Protocol:** SPI
- **IDE:** Visual Studio 2022
- **Target device:** ESP32

---

## How It Works

1. ESP32 initializes three SPI devices simultaneously
2. Every second, MAX6675 sensor is polled for temperature
3. Value is displayed on the 7-segment display via SN74HC595 shift register
4. All readings are stored in an ArrayList
5. On completion, data is written to a timestamped `.csv` file on the SD card
6. SD card is safely unmounted after write

## Demo

> 🎥 Video is available in the [Issues](../../issues) section of this repository.

## Author

**Aldarkmud**  
Technician Programmer student — Pułtusk, Poland  
Stack: C#, nanoFramework, Embedded Systems, ESP32, SPI

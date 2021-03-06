﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace SplasherStudio.Entities {
	public static class MemoryReader {
		public static T Read<T>(this Process targetProcess, IntPtr address, params int[] offsets) where T : struct {
			if (targetProcess == null || targetProcess.HasExited || address == IntPtr.Zero) { return default(T); }

			int last = OffsetAddress(targetProcess, ref address, offsets);

			Type type = typeof(T);
			type = (type.IsEnum ? Enum.GetUnderlyingType(type) : type);

			int count = (type == typeof(bool)) ? 1 : Marshal.SizeOf(type);
			byte[] buffer = Read(targetProcess, address + last, count);

			object obj = ResolveToType(buffer, type);
			return (T)((object)obj);
		}
		private static object ResolveToType(byte[] bytes, Type type) {
			if (type == typeof(int)) {
				return BitConverter.ToInt32(bytes, 0);
			} else if (type == typeof(uint)) {
				return BitConverter.ToUInt32(bytes, 0);
			} else if (type == typeof(float)) {
				return BitConverter.ToSingle(bytes, 0);
			} else if (type == typeof(double)) {
				return BitConverter.ToDouble(bytes, 0);
			} else if (type == typeof(byte)) {
				return bytes[0];
			} else if (type == typeof(bool)) {
				return bytes != null && bytes[0] > 0;
			} else if (type == typeof(short)) {
				return BitConverter.ToInt16(bytes, 0);
			} else if (type == typeof(ushort)) {
				return BitConverter.ToUInt16(bytes, 0);
			} else if (type == typeof(long)) {
				return BitConverter.ToInt64(bytes, 0);
			} else if (type == typeof(ulong)) {
				return BitConverter.ToUInt64(bytes, 0);
			} else {
				GCHandle gCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
				try {
					return Marshal.PtrToStructure(gCHandle.AddrOfPinnedObject(), type);
				} finally {
					gCHandle.Free();
				}
			}
		}
		public static byte[] Read(this Process targetProcess, IntPtr address, int numBytes) {
			byte[] buffer = new byte[numBytes];
			if (targetProcess == null || targetProcess.HasExited || address == IntPtr.Zero) { return buffer; }

			int bytesRead;
			WinAPI.ReadProcessMemory(targetProcess.Handle, address, buffer, numBytes, out bytesRead);
			return buffer;
		}
		public static byte[] Read(this Process targetProcess, IntPtr address, int numBytes, params int[] offsets) {
			byte[] buffer = new byte[numBytes];
			if (targetProcess == null || targetProcess.HasExited || address == IntPtr.Zero) { return buffer; }

			int last = OffsetAddress(targetProcess, ref address, offsets);

			int bytesRead;
			WinAPI.ReadProcessMemory(targetProcess.Handle, address + last, buffer, numBytes, out bytesRead);
			return buffer;
		}
		public static string Read(this Process targetProcess, IntPtr address, bool is64bit = false) {
			if (targetProcess == null || targetProcess.HasExited || address == IntPtr.Zero) { return string.Empty; }

			int length = Read<int>(targetProcess, address, is64bit ? 0x10 : 0x8);
			return Encoding.Unicode.GetString(Read(targetProcess, address + (is64bit ? 0x14 : 0xc), 2 * length));
		}
		public static string ReadAscii(this Process targetProcess, IntPtr address) {
			if (targetProcess == null || targetProcess.HasExited || address == IntPtr.Zero) { return string.Empty; }

			StringBuilder sb = new StringBuilder();
			byte[] data = new byte[128];
			int bytesRead;
			int offset = 0;
			bool invalid = false;
			do {
				WinAPI.ReadProcessMemory(targetProcess.Handle, address + offset, data, 128, out bytesRead);
				int i = 0;
				while (i < bytesRead) {
					byte d = data[i++];
					if (d == 0) {
						i--;
						break;
					} else if (d > 127) {
						invalid = true;
						break;
					}
				}
				if (i > 0) {
					sb.Append(Encoding.ASCII.GetString(data, 0, i));
				}
				if (i < bytesRead || invalid) {
					break;
				}
				offset += 128;
			} while (bytesRead > 0);

			return invalid ? string.Empty : sb.ToString();
		}
		public static void Write<T>(this Process targetProcess, IntPtr address, T value, params int[] offsets) where T : struct {
			if (targetProcess == null || targetProcess.HasExited) { return; }

			int last = OffsetAddress(targetProcess, ref address, offsets);
			byte[] buffer = null;
			if (typeof(T) == typeof(bool)) {
				buffer = BitConverter.GetBytes(Convert.ToBoolean(value));
			} else if (typeof(T) == typeof(byte)) {
				buffer = BitConverter.GetBytes(Convert.ToByte(value));
			} else if (typeof(T) == typeof(int)) {
				buffer = BitConverter.GetBytes(Convert.ToInt32(value));
			} else if (typeof(T) == typeof(uint)) {
				buffer = BitConverter.GetBytes(Convert.ToUInt32(value));
			} else if (typeof(T) == typeof(short)) {
				buffer = BitConverter.GetBytes(Convert.ToInt16(value));
			} else if (typeof(T) == typeof(ushort)) {
				buffer = BitConverter.GetBytes(Convert.ToUInt16(value));
			} else if (typeof(T) == typeof(long)) {
				buffer = BitConverter.GetBytes(Convert.ToInt64(value));
			} else if (typeof(T) == typeof(ulong)) {
				buffer = BitConverter.GetBytes(Convert.ToUInt64(value));
			} else if (typeof(T) == typeof(float)) {
				buffer = BitConverter.GetBytes(Convert.ToSingle(value));
			} else if (typeof(T) == typeof(double)) {
				buffer = BitConverter.GetBytes(Convert.ToDouble(value));
			}

			int bytesWritten;
			WinAPI.WriteProcessMemory(targetProcess.Handle, address + last, buffer, buffer.Length, out bytesWritten);
		}
		private static int OffsetAddress(this Process targetProcess, ref IntPtr address, params int[] offsets) {
			bool is64bit = Is64Bit(targetProcess);
			byte[] buffer = new byte[is64bit ? 8 : 4];
			int bytesWritten;
			for (int i = 0; i < offsets.Length - 1; i++) {
				WinAPI.ReadProcessMemory(targetProcess.Handle, address + offsets[i], buffer, buffer.Length, out bytesWritten);
				if (is64bit) {
					address = (IntPtr)BitConverter.ToUInt64(buffer, 0);
				} else {
					address = (IntPtr)BitConverter.ToUInt32(buffer, 0);
				}
			}
			return offsets.Length > 0 ? offsets[offsets.Length - 1] : 0;
		}
		public static bool Is64Bit(this Process process) {
			if (process == null || process.HasExited) { return false; }
			bool flag;
			WinAPI.IsWow64Process(process.Handle, out flag);
			return Environment.Is64BitOperatingSystem && !flag;
		}
		public static IntPtr[] FindSignatures(this Process targetProcess, params string[] searchStrings) {
			IntPtr[] returnAddresses = new IntPtr[searchStrings.Length];
			MemorySignature[] byteCodes = new MemorySignature[searchStrings.Length];
			for (int i = 0; i < searchStrings.Length; i++) {
				byteCodes[i] = GetSignature(searchStrings[i]);
			}

			long minAddress = 65536;
			long maxAddress = Is64Bit(targetProcess) ? 140737488289791L : 2147418111L;
			uint memInfoSize = (uint)Marshal.SizeOf(typeof(MemInfo));
			MemInfo memInfo;

			int foundAddresses = 0;
			while (minAddress < maxAddress && foundAddresses < searchStrings.Length) {
				WinAPI.VirtualQueryEx(targetProcess.Handle, (IntPtr)minAddress, out memInfo, memInfoSize);
				long regionSize = (long)memInfo.RegionSize;
				if (regionSize <= 0) { break; }

				if ((memInfo.Protect & 0x40) != 0 && (memInfo.Type & 0x20000) != 0 && memInfo.State == 0x1000) {
					byte[] buffer = new byte[regionSize];

					int bytesRead = 0;
					if (WinAPI.ReadProcessMemory(targetProcess.Handle, memInfo.BaseAddress, buffer, (int)regionSize, out bytesRead)) {
						for (int i = 0; i < searchStrings.Length; i++) {
							if (returnAddresses[i] == IntPtr.Zero) {
								if (SearchMemory(buffer, byteCodes[i], (IntPtr)minAddress, ref returnAddresses[i])) {
									foundAddresses++;
								}
							}
						}
					}
				}

				minAddress += regionSize;
			}

			return returnAddresses;
		}
		private static bool SearchMemory(byte[] buffer, MemorySignature byteCode, IntPtr currentAddress, ref IntPtr foundAddress) {
			byte[] bytes = byteCode.byteCode;
			byte[] wild = byteCode.wildCards;
			for (int i = 0, j = 0; i <= buffer.Length - bytes.Length; i++) {
				int k = i;
				while (j < bytes.Length && (wild[j] == 1 || buffer[k] == bytes[j])) {
					k++; j++;
				}
				if (j == bytes.Length) {
					foundAddress = currentAddress + i + bytes.Length + byteCode.offset;
					return true;
				}
				j = 0;
			}
			return false;
		}
		private static MemorySignature GetSignature(string searchString) {
			int offsetIndex = searchString.IndexOf("|");
			offsetIndex = offsetIndex < 0 ? searchString.Length : offsetIndex;

			if (offsetIndex % 2 != 0) {
				Console.WriteLine(searchString + " is of odd length.");
				return null;
			}

			byte[] byteCode = new byte[offsetIndex / 2];
			byte[] wildCards = new byte[offsetIndex / 2];
			for (int i = 0, j = 0; i < offsetIndex; i++) {
				byte temp = (byte)(((int)searchString[i] - 0x30) & 0x1F);
				byteCode[j] |= temp > 0x09 ? (byte)(temp - 7) : temp;
				if (searchString[i] == '?') {
					wildCards[j] = 1;
				}
				if ((i & 1) == 1) {
					j++;
				} else {
					byteCode[j] <<= 4;
				}
			}
			int offset = 0;
			if (offsetIndex < searchString.Length) {
				int.TryParse(searchString.Substring(offsetIndex + 1), out offset);
			}
			return new MemorySignature(byteCode, wildCards, offset);
		}
		private class MemorySignature {
			public byte[] byteCode;
			public byte[] wildCards;
			public int offset;

			public MemorySignature(byte[] byteCode, byte[] wildCards, int offset) {
				this.byteCode = byteCode;
				this.wildCards = wildCards;
				this.offset = offset;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MemInfo {
			public IntPtr BaseAddress;
			public IntPtr AllocationBase;
			public uint AllocationProtect;
			public IntPtr RegionSize;
			public uint State;
			public uint Protect;
			public uint Type;
		}
		private static class WinAPI {
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MemInfo lpBuffer, uint dwLength);
			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);
		}
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct ModuleInfo {
		public IntPtr BaseAddress;
		public uint ModuleSize;
		public IntPtr EntryPoint;
	}
}
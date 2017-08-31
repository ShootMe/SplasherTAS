using System;
using System.Diagnostics;
namespace SplasherStudio.Entities {
	public class SplasherMemory {
		private static ProgramPointer GameData = new ProgramPointer(true, new ProgramSignature(PointerVersion.V1, "55488BEC564883EC08488BF1488B0425????????488B4018488B4018F30F1005EC000000F30F5AC0F30F100DD0000000F30F5AC9F30F105610F30F5AD2F20F5CCAF20F59C1488BC8|-56"));
		private static ProgramPointer GameManager = new ProgramPointer(false, new ProgramSignature(PointerVersion.V1, "55488BEC564883EC08488BF1F30F10050C030000F30F5AC0F20F5AC04883EC2049BB????????????????41FFD34883C420B8"));
		private static ProgramPointer ChronoHUD = new ProgramPointer(false, new ProgramSignature(PointerVersion.V1, "55488BEC564883EC18488BF1B8????????488930488B4648488945E8488B0425????????488B4018488B80|-30"));
		private static ProgramPointer TAS = new ProgramPointer(false, new ProgramSignature(PointerVersion.V1, "55488BEC4883EC10B9????????4883EC2049BB????????????????41FFD34883C420488945F8488BC8BA????????4883EC2049BB????????????????41FFD34883C420488B4DF8B8????????48890833C94883EC20|-13"));
		private static ProgramPointer PlayerController = new ProgramPointer(false, new ProgramSignature(PointerVersion.V1, "55488BEC56415641574883EC68488BF14883EC2049BB????????????????41FFD34883C420488BC883390041BA????????4883EC2049BB????????????????41FFD34883C420488986????????41BA????????488BCE4883EC2049BB????????????????41FFD34883C420488986????????B8"));
		public Process Program { get; set; }
		public bool IsHooked { get; set; } = false;
		private DateTime lastHooked;

		public SplasherMemory() {
			lastHooked = DateTime.MinValue;
		}

		public PlayerState PlayerState() {
			//PlayerController.Instance.State
			return (PlayerState)PlayerController.Read<int>(Program, 0x0, 0x3f0);
		}
		public Vector2 PlayerVelocity() {
			//PlayerController.Instance._velocity
			float x = PlayerController.Read<float>(Program, 0x0, 0x410);
			float y = PlayerController.Read<float>(Program, 0x0, 0x414);
			return new Vector2(x, y);
		}
		public Vector2 PlayerPosition() {
			//PlayerController.Instance._position
			float x = PlayerController.Read<float>(Program, 0x0, 0x4f4);
			float y = PlayerController.Read<float>(Program, 0x0, 0x4f8);
			return new Vector2(x, y);
		}
		public string SceneName() {
			//GameData.Instance.CurrentLevelMetaData.SceneName
			return GameData.Read(Program, 0x208, 0x18);
		}
		public string LevelName() {
			//GameData.Instance.CurrentLevelMetaData.LevelName.Id
			return GameData.Read(Program, 0x208, 0x20, 0x10);
		}
		public int Checkpoints() {
			//GameData.Instance.CurrentLevelData.CheckpointCount
			return GameData.Read<int>(Program, 0x200, 0x38);
		}
		public int CurrentCheckpoint() {
			//GameManager.Instance.CheckpointIndex
			return GameManager.Read<int>(Program, 0x0, 0xd8);
		}
		public float PBTime(int checkpoint) {
			//GameData.Instance.CurrentLevelData.PersonalBestTimes[checkpoint]
			return GameData.Read<float>(Program, 0x200, 0x20, 0x20 + 0x4 * checkpoint);
		}
		public float CurrentTime(int checkpoint) {
			//GameData.Instance.CurrentLevelData.currentTimes[checkpoint]
			return GameData.Read<float>(Program, 0x200, 0x28, 0x20 + 0x4 * checkpoint);
		}
		public bool Paused() {
			//GameManager.Instance.paused
			return GameManager.Read<bool>(Program, 0x0, 0xa0);
		}
		public LockControlType ControlLock() {
			//GameManager._lockControl
			return (LockControlType)GameManager.Read<int>(Program, 0xc);
		}
		public GameMode GameMode() {
			//GameManager.Mode
			return (GameMode)GameManager.Read<int>(Program, 0x10);
		}
		public ChronometerState ChronoState() {
			//ChronoHUD.State
			return (ChronometerState)ChronoHUD.Read<int>(Program, 0xc);
		}
		public float ElapsedTime() {
			//ChronoHUD.elapsedTime
			return (float)(int)(ChronoHUD.Read<float>(Program, 0x10) * 100f) / 100f;
		}
		public float ElapsedTimeRaw() {
			//ChronoHUD.elapsedTime
			return ChronoHUD.Read<float>(Program, 0x10);
		}
		public string TASOutput() {
			return TAS.Read(Program, 0x20);
		}
		public string TASScene() {
			return TAS.Read(Program, 0x28);
		}
		public bool HookProcess() {
			if ((Program == null || Program.HasExited) && DateTime.Now > lastHooked.AddSeconds(1)) {
				lastHooked = DateTime.Now;
				Process[] processes = Process.GetProcessesByName("Splasher");
				Program = processes.Length == 0 ? null : processes[0];
			}

			IsHooked = Program != null && !Program.HasExited;

			return IsHooked;
		}
		public void Dispose() {
			if (Program != null) {
				Program.Dispose();
			}
		}
	}
	public enum PointerVersion {
		V1
	}
	public class ProgramSignature {
		public PointerVersion Version { get; set; }
		public string Signature { get; set; }
		public ProgramSignature(PointerVersion version, string signature) {
			Version = version;
			Signature = signature;
		}
		public override string ToString() {
			return Version.ToString() + " - " + Signature;
		}
	}
	public class ProgramPointer {
		private int lastID;
		private DateTime lastTry;
		private ProgramSignature[] signatures;
		private int[] offsets;
		private bool is64bit;
		public IntPtr Pointer { get; private set; }
		public PointerVersion Version { get; private set; }
		public bool AutoDeref { get; private set; }

		public ProgramPointer(bool autoDeref, params ProgramSignature[] signatures) {
			AutoDeref = autoDeref;
			this.signatures = signatures;
			lastID = -1;
			lastTry = DateTime.MinValue;
		}
		public ProgramPointer(bool autoDeref, params int[] offsets) {
			AutoDeref = autoDeref;
			this.offsets = offsets;
			lastID = -1;
			lastTry = DateTime.MinValue;
		}

		public T Read<T>(Process program, params int[] offsets) where T : struct {
			GetPointer(program);
			return program.Read<T>(Pointer, offsets);
		}
		public string Read(Process program, params int[] offsets) {
			GetPointer(program);
			IntPtr ptr = (IntPtr)program.Read<uint>(Pointer, offsets);
			return program.Read(ptr, is64bit);
		}
		public void Write<T>(Process program, T value, params int[] offsets) where T : struct {
			GetPointer(program);
			program.Write<T>(Pointer, value, offsets);
		}
		public IntPtr GetPointer(Process program) {
			if ((program?.HasExited).GetValueOrDefault(true)) {
				Pointer = IntPtr.Zero;
				lastID = -1;
				return Pointer;
			} else if (program.Id != lastID) {
				Pointer = IntPtr.Zero;
				lastID = program.Id;
			}

			if (Pointer == IntPtr.Zero && DateTime.Now > lastTry.AddSeconds(1)) {
				lastTry = DateTime.Now;

				Pointer = GetVersionedFunctionPointer(program);
				if (Pointer != IntPtr.Zero) {
					is64bit = program.Is64Bit();
					Pointer = (IntPtr)program.Read<uint>(Pointer);
					if (AutoDeref) {
						if (is64bit) {
							Pointer = (IntPtr)program.Read<ulong>(Pointer);
						} else {
							Pointer = (IntPtr)program.Read<uint>(Pointer);
						}
					}
				}
			}
			return Pointer;
		}
		private IntPtr GetVersionedFunctionPointer(Process program) {
			if (signatures != null) {
				for (int i = 0; i < signatures.Length; i++) {
					ProgramSignature signature = signatures[i];

					IntPtr ptr = program.FindSignatures(signature.Signature)[0];
					if (ptr != IntPtr.Zero) {
						Version = signature.Version;
						return ptr;
					}
				}
			} else {
				IntPtr ptr = (IntPtr)program.Read<uint>(program.MainModule.BaseAddress, offsets);
				if (ptr != IntPtr.Zero) {
					return ptr;
				}
			}

			return IntPtr.Zero;
		}
	}
}

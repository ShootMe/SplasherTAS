using System;
using System.Text;
namespace TAS {
	[Flags]
	public enum Actions {
		None,
		Left = 1,
		Right = 2,
		Up = 4,
		Down = 8,
		Jump = 16,
		Water = 32,
		Goo = 64,
		Bouncy = 128,
		Start = 256,
		Select = 512,
		LeftBumper = 1024,
		RightBumper = 2048,
		Angle = 4096
	}
	public class InputRecord {
		public int Line { get; set; }
		public int Frames { get; set; }
		public Actions Actions { get; set; }
		public float Angle { get; set; }

		public InputRecord() { }
		public InputRecord(int number, string line) {
			Line = number;

			int index = 0;
			Frames = ReadFrames(line, ref index);
			if (Frames == 0) { return; }

			while (index < line.Length) {
				char c = line[index];

				switch (char.ToUpper(c)) {
					case 'L': Actions ^= Actions.Left; break;
					case 'R': Actions ^= Actions.Right; break;
					case 'U': Actions ^= Actions.Up; break;
					case 'D': Actions ^= Actions.Down; break;
					case 'J': Actions ^= Actions.Jump; break;
					case 'W': Actions ^= Actions.Water; break;
					case 'G': Actions ^= Actions.Goo; break;
					case 'B': Actions ^= Actions.Bouncy; break;
					case 'S': Actions ^= Actions.Start; break;
					case 'X': Actions ^= Actions.Select; break;
					case '[': Actions ^= Actions.LeftBumper; break;
					case ']': Actions ^= Actions.RightBumper; break;
					case 'A':
						Actions ^= Actions.Angle;
						index++;
						Angle = ReadAngle(line, ref index);
						continue;
				}

				index++;
			}

			if (HasActions(Actions.Angle)) {
				Actions &= ~Actions.Right & ~Actions.Left & ~Actions.Up & ~Actions.Down;
			} else {
				Angle = 0;
			}
			if (HasActions(Actions.Bouncy)) {
				Actions &= ~Actions.Water & ~Actions.Goo;
			} else if (HasActions(Actions.Water)) {
				Actions &= ~Actions.Goo;
			}
		}
		private int ReadFrames(string line, ref int start) {
			bool foundFrames = false;
			int frames = 0;

			while (start < line.Length) {
				char c = line[start];

				if (!foundFrames) {
					if (char.IsDigit(c)) {
						foundFrames = true;
						frames = c ^ 0x30;
					} else if (c != ' ') {
						return frames;
					}
				} else if (char.IsDigit(c)) {
					if (frames < 9999) {
						frames = frames * 10 + (c ^ 0x30);
					} else {
						frames = 9999;
					}
				} else if (c != ' ') {
					return frames;
				}

				start++;
			}

			return frames;
		}
		private float ReadAngle(string line, ref int start) {
			bool foundAngle = false;
			bool foundDecimal = false;
			int decimalPlaces = 1;
			int angle = 0;
			bool negative = false;

			while (start < line.Length) {
				char c = line[start];

				if (!foundAngle) {
					if (char.IsDigit(c)) {
						foundAngle = true;
						angle = c ^ 0x30;
					} else if (c == '.') {
						foundAngle = true;
						foundDecimal = true;
					} else if (c == '-') {
						negative = true;
					}
				} else if (char.IsDigit(c)) {
					angle = angle * 10 + (c ^ 0x30);
					if (foundDecimal) {
						decimalPlaces *= 10;
					}
				} else if (c == '.') {
					foundDecimal = true;
				} else if (c != ' ') {
					return (negative ? (float)-angle : (float)angle) / (float)decimalPlaces;
				}

				start++;
			}

			return (negative ? (float)-angle : (float)angle) / (float)decimalPlaces;
		}
		public float GetX() {
			if (HasActions(Actions.Right)) {
				return 1f;
			} else if (HasActions(Actions.Left)) {
				return -1f;
			} else if (!HasActions(Actions.Angle)) {
				return 0f;
			}
			float x = (float)Math.Sin(Angle * Math.PI / 180.0);
			if (Math.Abs(x) < 0.1f) {
				return 0f;
			}
			return x;
		}
		public float GetXMax() {
			float x = GetX();
			if (x < -0.1f) {
				return -1f;
			} else if (x > 0.1f) {
				return 1f;
			}
			return 0;
		}
		public float GetY() {
			if (HasActions(Actions.Up)) {
				return 1f;
			} else if (HasActions(Actions.Down)) {
				return -1f;
			} else if (!HasActions(Actions.Angle)) {
				return 0f;
			}
			float y = (float)Math.Cos(Angle * Math.PI / 180.0);
			if (Math.Abs(y) < 0.1f) {
				return 0f;
			}
			return y;
		}
		public float GetYMax() {
			float y = GetY();
			if (y < -0.1f) {
				return -1f;
			} else if (y > 0.1f) {
				return 1f;
			}
			return 0;
		}
		public bool HasActions(Actions actions) {
			return (Actions & actions) != 0;
		}
		public override string ToString() {
			return Frames == 0 ? string.Empty : Frames.ToString().PadLeft(4, ' ') + ActionsToString();
		}
		public string ActionsToString() {
			StringBuilder sb = new StringBuilder();
			if (HasActions(Actions.Left)) { sb.Append(",L"); }
			if (HasActions(Actions.Right)) { sb.Append(",R"); }
			if (HasActions(Actions.Up)) { sb.Append(",U"); }
			if (HasActions(Actions.Down)) { sb.Append(",D"); }
			if (HasActions(Actions.Jump)) { sb.Append(",J"); }
			if (HasActions(Actions.Water)) { sb.Append(",W"); }
			if (HasActions(Actions.Goo)) { sb.Append(",G"); }
			if (HasActions(Actions.Bouncy)) { sb.Append(",B"); }
			if (HasActions(Actions.Start)) { sb.Append(",S"); }
			if (HasActions(Actions.Select)) { sb.Append(",X"); }
			if (HasActions(Actions.LeftBumper)) { sb.Append(",["); }
			if (HasActions(Actions.RightBumper)) { sb.Append(",]"); }
			if (HasActions(Actions.Angle)) { sb.Append(",A,").Append(Angle.ToString("0")); }
			return sb.ToString();
		}
		public override bool Equals(object obj) {
			return obj is InputRecord && ((InputRecord)obj) == this;
		}
		public override int GetHashCode() {
			return Frames ^ (int)Actions;
		}
		public static bool operator ==(InputRecord one, InputRecord two) {
			bool oneNull = (object)one == null;
			bool twoNull = (object)two == null;
			if (oneNull != twoNull) {
				return false;
			} else if (oneNull && twoNull) {
				return true;
			}
			return one.Actions == two.Actions && one.Angle == two.Angle;
		}
		public static bool operator !=(InputRecord one, InputRecord two) {
			bool oneNull = (object)one == null;
			bool twoNull = (object)two == null;
			if (oneNull != twoNull) {
				return true;
			} else if (oneNull && twoNull) {
				return false;
			}
			return one.Actions != two.Actions || one.Angle != two.Angle;
		}
	}
}

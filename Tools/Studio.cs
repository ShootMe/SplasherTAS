using SplasherStudio.Entities;
using SplasherStudio.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
using Microsoft.Win32;
namespace SplasherStudio {
	public partial class Studio : Form {
		private static string titleBarText = "Studio v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
		private const string RegKey = "HKEY_CURRENT_USER\\SOFTWARE\\SplasherStudio\\Form";
		[STAThread]
		public static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Studio());
		}

		private List<InputRecord> Lines = new List<InputRecord>();
		private SplasherMemory memory = new SplasherMemory();
		private int totalFrames = 0, currentFrame = 0;
		private bool updating = false;
		private DateTime lastChanged = DateTime.MinValue;
		public Studio() {
			InitializeComponent();
			Text = titleBarText;

			Lines.Add(new InputRecord(""));
			EnableStudio(false);

			Thread updateThread = new Thread(UpdateLoop);
			updateThread.IsBackground = true;
			updateThread.Start();

			DesktopLocation = new Point(RegRead("x", DesktopLocation.X), RegRead("y", DesktopLocation.Y));
			Size = new Size(RegRead("w", Size.Width), RegRead("h", Size.Height));
		}
		private void TASStudio_FormClosed(object sender, FormClosedEventArgs e) {
			RegWrite("x", DesktopLocation.X); RegWrite("y", DesktopLocation.Y);
			RegWrite("w", Size.Width); RegWrite("h", Size.Height);
		}
		private void TASStudio_KeyDown(object sender, KeyEventArgs e) {
			try {
				if (e.Modifiers == (Keys.Shift | Keys.Control) && e.KeyCode == Keys.S) {
					tasText.SaveNewFile();
				} else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.S) {
					tasText.SaveFile();
				} else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.O) {
					tasText.OpenFile();
				}
			} catch (Exception ex) {
				MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void UpdateLoop() {
			bool lastHooked = false;
			while (true) {
				try {
					bool hooked = memory.HookProcess();
					if (lastHooked != hooked) {
						lastHooked = hooked;
						this.Invoke((Action)delegate () { EnableStudio(hooked); });
					}
					if (lastChanged.AddSeconds(0.6) < DateTime.Now) {
						lastChanged = DateTime.Now;
						this.Invoke((Action)delegate () {
							if ((!string.IsNullOrEmpty(tasText.LastFileName) || !string.IsNullOrEmpty(tasText.SaveToFileName)) && tasText.IsChanged) {
								tasText.SaveFile();
							}
						});
					}
					if (hooked) {
						UpdateValues();
					}

					Thread.Sleep(12);
				} catch { }
			}
		}
		public void EnableStudio(bool hooked) {
			if (hooked) {
				string fileName = Path.Combine(Path.GetDirectoryName(memory.Program.MainModule.FileName), "Splasher.tas");
				if (string.IsNullOrEmpty(tasText.LastFileName)) {
					if (string.IsNullOrEmpty(tasText.SaveToFileName)) {
						tasText.OpenBindingFile(fileName, Encoding.ASCII);
					}
					tasText.LastFileName = fileName;
				}
				tasText.SaveToFileName = fileName;
				if (tasText.LastFileName != tasText.SaveToFileName) {
					tasText.SaveFile(true);
				}
				tasText.Focus();
			} else {
				lblStatus.Text = "Searching...";
			}
		}
		public void UpdateValues() {
			if (this.InvokeRequired) {
				this.Invoke((Action)UpdateValues);
			} else {
				string tas = memory.TASOutput();
				if (!string.IsNullOrEmpty(tas)) {
					int index = tas.IndexOf('[');
					string num = tas.Substring(0, index);
					int temp = 0;
					if (int.TryParse(num, out temp)) {
						temp--;
						if (tasText.CurrentLine != temp) {
							tasText.CurrentLine = temp;
						}
					}

					index = tas.IndexOf(':');
					num = tas.Substring(index + 2, tas.IndexOf(')', index) - index - 2);
					if (int.TryParse(num, out temp)) {
						currentFrame = temp;
					}

					index = tas.IndexOf('(');
					int index2 = tas.IndexOf(' ', index);
					num = tas.Substring(index + 1, index2 - index - 1);
					if (tasText.CurrentLineText != num) {
						tasText.CurrentLineText = num;
					}
				} else {
					currentFrame = 0;
					if (tasText.CurrentLine >= 0) {
						tasText.CurrentLine = -1;
					}
				}

				UpdateStatusBar();
			}
		}
		private void tasText_LineRemoved(object sender, LineRemovedEventArgs e) {
			int count = e.Count;
			while (count-- > 0) {
				InputRecord input = Lines[e.Index];
				totalFrames -= input.Frames;
				Lines.RemoveAt(e.Index);
			}

			UpdateStatusBar();
		}
		private void tasText_LineInserted(object sender, LineInsertedEventArgs e) {
			RichText tas = (RichText)sender;
			int count = e.Count;
			while (count-- > 0) {
				InputRecord input = new InputRecord(tas.GetLineText(e.Index + count));
				Lines.Insert(e.Index, input);
				totalFrames += input.Frames;
			}

			UpdateStatusBar();
		}
		private void UpdateStatusBar() {
			if (memory.IsHooked) {
				lblStatus.Text = "F(" + (currentFrame > 0 ? currentFrame + "/" : "") + totalFrames + ")(" + memory.ControlLock().ToString() + ")(" + memory.PlayerState().ToString() + ")\r\n" + memory.PlayerPosition().ToString() + memory.PlayerVelocity().ToString();
			} else {
				lblStatus.Text = "F(" + totalFrames + ")\r\nSearching...";
			}
		}
		private void tasText_TextChanged(object sender, TextChangedEventArgs e) {
			lastChanged = DateTime.Now;
			UpdateLines((RichText)sender, e.ChangedRange);
		}
		private void UpdateLines(RichText tas, Range range) {
			if (updating) { return; }
			updating = true;

			int start = range.Start.iLine;
			int end = range.End.iLine;
			while (start <= end) {
				InputRecord old = Lines.Count > start ? Lines[start] : null;

				string text = tas[start++].Text;

				InputRecord input = new InputRecord(text);
				if (old != null) {
					totalFrames -= old.Frames;

					string line = input.ToString();
					if (text != line) {
						if (old.Frames == 0 && old.ZeroPadding == input.ZeroPadding && old.Equals(input) && line.Length >= text.Length) {
							line = string.Empty;
						}
						Range oldRange = tas.Selection.Clone();
						tas.Selection = tas.GetLine(start - 1);
						tas.SelectedText = line;

						int actionPosition = input.ActionPosition();
						if (!string.IsNullOrEmpty(line)) {
							int index = oldRange.Start.iChar + line.Length - text.Length;
							if (index < 0) { index = 0; }
							if (index > 4 && old.Angle == input.Angle) { index = 4; }
							if (old.Frames == input.Frames && old.ZeroPadding == input.ZeroPadding && old.Angle == input.Angle) { index = 4; }

							tas.Selection.Start = new Place(index, start - 1);
						}

						Text = titleBarText + " ***";
					}
					Lines[start - 1] = input;
				}

				totalFrames += input.Frames;
			}

			UpdateStatusBar();

			updating = false;
		}
		private void tasText_NoChanges(object sender, EventArgs e) {
			Text = titleBarText;
		}
		private void tasText_FileOpening(object sender, EventArgs e) {
			Lines.Clear();
			totalFrames = 0;
			UpdateStatusBar();
		}
		private void tasText_FileOpened(object sender, EventArgs e) {
			try {
				tasText.SaveFile(true);
			} catch { }
		}
		private int RegRead(string name, int def) {
			object o = null;
			try {
				o = Registry.GetValue(RegKey, name, null);
			} catch { }

			if (o is int) {
				return (int)o;
			}

			return def;
		}
		private void RegWrite(string name, int val) {
			try {
				Registry.SetValue(RegKey, name, val);
			} catch { }
		}
	}
}
using J2i.Net.XInputWrapper;
using System;
using System.Threading;
using TSKGames.Inputs;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace TAS {
	[Flags]
	public enum State {
		None = 0,
		Enable = 1,
		Record = 2,
		FrameStep = 4
	}
	public class Manager {
		public static bool Running, Recording;
		private static InputController controller;
		private static State state, nextState;
		private static int frameRate;
		private static XboxController xbox;
		public static string CurrentStatus;
		public static string NextScene;
		static Manager() {
			controller = new InputController("Splasher.tas");
			xbox = XboxController.RetrieveController(0);
			XboxController.UpdateFrequency = 30;
			XboxController.StartPolling();
		}
		public static void Main(string[] args) {
			controller.ReloadPlayback();

			Console.WriteLine("Finished");
			Console.ReadLine();
		}
		public static bool IsLoading() {
			Scene scene = SceneManager.GetActiveScene();
			return scene == null || (SceneManager.NextScene != scene.name && SceneManager.NextScene != scene.buildIndex.ToString());
		}
		public static void UpdateInputs() {
			HandleFrameRates();
			CheckControls();
			FrameStepping();
			NextScene = SceneManager.NextScene;

			if (HasFlag(state, State.Enable)) {
				Running = true;
				if (HasFlag(state, State.Record)) {
					controller.RecordPlayer();
				} else {
					controller.PlaybackPlayer();

					if (!controller.CanPlayback) {
						DisableRun();
					}
				}
				string status = controller.Current.Line + "[" + controller.ToString() + "]";
				CurrentStatus = status;
			} else {
				Running = false;
				CurrentStatus = null;
			}
		}
		public static float GetAxis(string axisName) {
			InputRecord input = controller.Current;
			switch (axisName) {
				case "Horizontal": return -input.GetX();
				case "Vertical": return -input.GetY();
				case "DPadX": return input.GetXMax();
				case "DPadY": return input.GetYMax();
				case "LeftStickX": return input.GetX();
				case "LeftStickY": return input.GetY();
			}
			return 0;
		}
		public static float GetAxisRaw(string axisName) {
			InputRecord input = controller.Current;
			switch (axisName) {
				case "Horizontal": return -input.GetX();
				case "Vertical": return -input.GetY();
				case "DPadX": return input.GetXMax();
				case "DPadY": return input.GetYMax();
				case "LeftStickX": return input.GetX();
				case "LeftStickY": return input.GetY();
			}
			return 0;
		}
		public static int GetAxisDown(string AxisName) {
			InputRecord input = controller.Current;
			switch (AxisName) {
				case "Horizontal": return -(int)input.GetXMax();
				case "Vertical": return -(int)input.GetYMax();
				case "DPadX": return (int)input.GetXMax();
				case "DPadY": return (int)input.GetYMax();
				case "LeftStickX": return (int)input.GetXMax();
				case "LeftStickY": return (int)input.GetYMax();
			}
			return 0;
		}
		public static bool GetButtonDown(InputGamepadButton button) {
			if (controller.CurrentInputFrame != 1) { return false; }

			InputRecord input = controller.Current;
			switch (button) {
				case InputGamepadButton.Action: return input.HasActions(Actions.Jump);
				case InputGamepadButton.Back: return input.HasActions(Actions.Goo);
				case InputGamepadButton.AltAction: return input.HasActions(Actions.Water);
				case InputGamepadButton.Menu: return input.HasActions(Actions.Bouncy);
				case InputGamepadButton.Start: return input.HasActions(Actions.Start);
				case InputGamepadButton.Select: return input.HasActions(Actions.Select);
				case InputGamepadButton.RB: return input.HasActions(Actions.RightBumper);
				case InputGamepadButton.LB: return input.HasActions(Actions.LeftBumper);
			}
			return false;
		}
		public static bool GetButton(InputGamepadButton button) {
			InputRecord input = controller.Current;
			switch (button) {
				case InputGamepadButton.Action: return input.HasActions(Actions.Jump);
				case InputGamepadButton.Back: return input.HasActions(Actions.Goo);
				case InputGamepadButton.AltAction: return input.HasActions(Actions.Water);
				case InputGamepadButton.Menu: return input.HasActions(Actions.Bouncy);
				case InputGamepadButton.Start: return input.HasActions(Actions.Start);
				case InputGamepadButton.Select: return input.HasActions(Actions.Select);
				case InputGamepadButton.RB: return input.HasActions(Actions.RightBumper);
				case InputGamepadButton.LB: return input.HasActions(Actions.LeftBumper);
			}
			return false;
		}
		private static void HandleFrameRates() {
			if (HasFlag(state, State.Enable) && !HasFlag(state, State.FrameStep) && !HasFlag(state, State.Record)) {
				float rightStickX = (float)xbox.RightThumbStickX / 32768f;

				if (rightStickX <= -0.9) {
					SetFrameRate(3);
				} else if (rightStickX <= -0.8) {
					SetFrameRate(6);
				} else if (rightStickX <= -0.7) {
					SetFrameRate(12);
				} else if (rightStickX <= -0.6) {
					SetFrameRate(16);
				} else if (rightStickX <= -0.5) {
					SetFrameRate(20);
				} else if (rightStickX <= -0.4) {
					SetFrameRate(28);
				} else if (rightStickX <= -0.3) {
					SetFrameRate(36);
				} else if (rightStickX <= -0.2) {
					SetFrameRate(44);
				} else if (rightStickX <= 0.2) {
					SetFrameRate();
				} else if (rightStickX <= 0.3) {
					SetFrameRate(80);
				} else if (rightStickX <= 0.4) {
					SetFrameRate(100);
				} else if (rightStickX <= 0.5) {
					SetFrameRate(120);
				} else if (rightStickX <= 0.6) {
					SetFrameRate(140);
				} else if (rightStickX <= 0.7) {
					SetFrameRate(160);
				} else if (rightStickX <= 0.8) {
					SetFrameRate(180);
				} else if (rightStickX <= 0.9) {
					SetFrameRate(200);
				} else {
					SetFrameRate(240);
				}
			} else {
				SetFrameRate();
			}
		}
		private static void SetFrameRate(int newFrameRate = 60) {
			if (frameRate == newFrameRate) { return; }

			frameRate = newFrameRate;
			Time.timeScale = (float)newFrameRate / 60f;
			Time.captureFramerate = newFrameRate;
			Application.targetFrameRate = newFrameRate;
			Time.fixedDeltaTime = 1f / 60f;
			Time.maximumDeltaTime = Time.fixedDeltaTime;
			QualitySettings.vSyncCount = 0;
		}
		private static void FrameStepping() {
			float rightStickX = (float)xbox.RightThumbStickX / 32768f;
			bool rightTrigger = xbox.RightTrigger == 255;
			bool dpadUp = xbox.IsDPadUpPressed;
			bool dpadDown = xbox.IsDPadDownPressed;

			if (HasFlag(state, State.Enable) && !HasFlag(state, State.Record) && (HasFlag(state, State.FrameStep) || dpadUp && !rightTrigger)) {
				bool continueLoop = dpadUp;
				while (HasFlag(state, State.Enable)) {
					rightStickX = (float)xbox.RightThumbStickX / 32768f;
					rightTrigger = xbox.RightTrigger == 255;
					dpadUp = xbox.IsDPadUpPressed;
					dpadDown = xbox.IsDPadDownPressed;

					CheckControls();
					if (!continueLoop && ((dpadUp && !rightTrigger))) {
						state |= State.FrameStep;
						break;
					} else if (dpadDown && !rightTrigger) {
						state &= ~State.FrameStep;
						break;
					} else if (rightStickX >= 0.2) {
						state |= State.FrameStep;
						int sleepTime = 0;
						if (rightStickX <= 0.3) {
							sleepTime = 200;
						} else if (rightStickX <= 0.4) {
							sleepTime = 100;
						} else if (rightStickX <= 0.5) {
							sleepTime = 80;
						} else if (rightStickX <= 0.6) {
							sleepTime = 64;
						} else if (rightStickX <= 0.7) {
							sleepTime = 48;
						} else if (rightStickX <= 0.8) {
							sleepTime = 32;
						} else if (rightStickX <= 0.9) {
							sleepTime = 16;
						}
						Thread.Sleep(sleepTime);
						break;
					}
					continueLoop = dpadUp;
					Thread.Sleep(1);
				}
				ReloadRun();
			}
		}
		private static void CheckControls() {
			bool leftStick = xbox.IsLeftStickPressed;
			bool rightStick = xbox.IsRightStickPressed;
			bool rightTrigger = xbox.RightTrigger >= 245;
			bool leftTrigger = xbox.LeftTrigger >= 245;
			bool dpadDown = xbox.IsDPadDownPressed;

			if (rightTrigger && leftTrigger) {
				if (!HasFlag(state, State.Enable) && rightStick) {
					nextState |= State.Enable;
				} else if (HasFlag(state, State.Enable) && dpadDown) {
					DisableRun();
				} else if (!HasFlag(state, State.Enable) && !HasFlag(state, State.Record) && leftStick) {
					nextState |= State.Record;
				}
			}

			if (!rightTrigger && !leftTrigger) {
				if (HasFlag(nextState, State.Enable)) {
					EnableRun();
				} else if (HasFlag(nextState, State.Record)) {
					RecordRun();
				}
			}
		}
		public static void GetCurrentInputs(InputRecord record) {
			float stick = InputGamePadMgr.GetAxisRaw("LeftStickX");
			float dpad = InputGamePadMgr.GetAxisRaw("DPadX");
			float axis = InputGamePadMgr.GetAxisRaw("Horizontal");
			float x = Mathf.Abs(stick) <= 0.1f ? (dpad == 0f ? (axis == 0f ? 0f : -axis) : dpad) : stick;

			stick = InputGamePadMgr.GetAxisRaw("LeftStickY");
			dpad = InputGamePadMgr.GetAxisRaw("DPadY");
			axis = InputGamePadMgr.GetAxisRaw("Vertical");
			float y = Mathf.Abs(stick) <= 0.1f ? (dpad == 0f ? (axis == 0f ? 0f : -axis) : dpad) : stick;

			float xMax = SetToMax(x, 0.1f, 1f);
			float yMax = SetToMax(y, 0.1f, 1f);

			if (xMax != 0 && yMax != 0) {
				record.Actions |= Actions.Angle;
				record.Angle = (int)(Math.Atan2(y, x) * 180 / Math.PI) + 180;
			} else if (xMax < 0) {
				record.Actions |= Actions.Left;
			} else if (xMax > 0) {
				record.Actions |= Actions.Right;
			} else if (yMax < 0) {
				record.Actions |= Actions.Down;
			} else if (yMax > 0) {
				record.Actions |= Actions.Up;
			}

			if (InputGamePadMgr.GetButton(InputGamepadButton.Menu)) {
				record.Actions |= Actions.Bouncy;
			}
			if (InputGamePadMgr.GetButton(InputGamepadButton.AltAction)) {
				record.Actions |= Actions.Water;
			}
			if (InputGamePadMgr.GetButton(InputGamepadButton.Back)) {
				record.Actions |= Actions.Goo;
			}
			if (InputGamePadMgr.GetButton(InputGamepadButton.Action)) {
				record.Actions |= Actions.Jump;
			}
			if (InputGamePadMgr.GetButton(InputGamepadButton.LB)) {
				record.Actions |= Actions.LeftBumper;
			}
			if (InputGamePadMgr.GetButton(InputGamepadButton.RB)) {
				record.Actions |= Actions.RightBumper;
			}
			if (InputGamePadMgr.GetButton(InputGamepadButton.Start)) {
				record.Actions |= Actions.Start;
			}
			if (InputGamePadMgr.GetButton(InputGamepadButton.Select)) {
				record.Actions |= Actions.Select;
			}
		}
		private static float SetToMax(float value, float min, float max) {
			if (value < -min) {
				return -max;
			} else if (value > min) {
				return max;
			}
			return 0f;
		}
		private static void DisableRun() {
			Running = false;
			Recording = false;
			state &= ~State.Enable;
			state &= ~State.FrameStep;
			state &= ~State.Record;
		}
		private static void EnableRun() {
			nextState &= ~State.Enable;
			UpdateVariables(false);
		}
		private static void RecordRun() {
			nextState &= ~State.Record;
			UpdateVariables(true);
		}
		private static void ReloadRun() {
			controller.ReloadPlayback();
		}
		private static void UpdateVariables(bool recording) {
			state |= State.Enable;
			state &= ~State.FrameStep;
			if (recording) {
				Recording = recording;
				state |= State.Record;
				controller.InitializeRecording();
			} else {
				state &= ~State.Record;
				controller.InitializePlayback();
			}
			Running = true;
		}
		private static bool HasFlag(State state, State flag) {
			return (state & flag) == flag;
		}
	}
}
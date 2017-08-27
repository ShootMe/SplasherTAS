namespace SplasherStudio.Entities {
	public class Vector2 {
		public float X { get; set; }
		public float Y { get; set; }
		public Vector2(float x, float y) {
			X = x;
			Y = y;
		}
		public override string ToString() {
			return "(" + X.ToString("0.00") + "," + Y.ToString("0.00") + ")";
		}
	}
}

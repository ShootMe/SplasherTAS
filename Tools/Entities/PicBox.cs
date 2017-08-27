using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace SplasherStudio.Entities {
	public class PicBox : PictureBox {
		[DefaultValue(100)]
		private int opacity;
		public int Opacity {
			get {
				return opacity;
			}
			set {
				opacity = value;
				if (brush != null) { brush.Dispose(); }
				brush = new SolidBrush(Color.FromArgb(Opacity * 255 / 100, DrawColor));
			}
		}
		private SolidBrush brush;
		private Color drawColor;
		public Color DrawColor {
			get { return drawColor; }
			set {
				drawColor = value;
				if (brush != null) { brush.Dispose(); }
				brush = new SolidBrush(Color.FromArgb(Opacity * 255 / 100, DrawColor));
			}
		}
		public PicBox() {
			Opacity = 100;
			DrawColor = Color.Transparent;
		}

		protected override void OnPaintBackground(PaintEventArgs e) {
			base.OnPaintBackground(e);
			Graphics g = e.Graphics;

			if (Parent != null) {
				if (BackColor != Color.Transparent) {
					BackColor = Color.Transparent;
				}
				int index = Parent.Controls.GetChildIndex(this);

				for (int i = Parent.Controls.Count - 1; i > index; i--) {
					Control c = Parent.Controls[i];
					if (c.Bounds.IntersectsWith(Bounds) && c.Visible && c.Width > 0 && c.Height > 0) {
						Bitmap bmp = new Bitmap(c.Width, c.Height, g);
						c.DrawToBitmap(bmp, c.ClientRectangle);
						g.DrawImageUnscaled(bmp, c.Left - Left, c.Top - Top);
						bmp.Dispose();
					}
				}
				g.FillRectangle(brush, this.ClientRectangle);
			} else {
				g.Clear(Color.Transparent);
				g.FillRectangle(brush, this.ClientRectangle);
			}
		}
		public override string ToString() {
			return "WTPicBox(" + Name + ")";
		}
	}
}
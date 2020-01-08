using System;
using System.Collections.Generic;
using System.Text;

namespace MoneyForwardViewer.DataBase.Tables {
	public class MfAsset {
		public DateTime Date {
			get;
			set;
		}

		public string Institution {
			get;
			set;
		}

		public string Category {
			get;
			set;
		}

		public int Amount {
			get;
			set;
		}
	}
}
